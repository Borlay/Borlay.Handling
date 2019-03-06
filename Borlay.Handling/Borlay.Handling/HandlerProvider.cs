using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public class HandlerProvider : IHandlerProvider
    {
        Dictionary<object, Dictionary<object, List<IHandler>>> handlers = new Dictionary<object, Dictionary<object, List<IHandler>>>();
        Dictionary<int, SemaphoreSlim> slims = new Dictionary<int, SemaphoreSlim>();

        public Resolver Resolver { get; private set; }

        public HandlerProvider()
        {
            this.Resolver = new Resolver();
        }

        public HandlerProvider(IResolver Resolver)
        {
            this.Resolver = new Resolver(Resolver);
        }

        public IHandler GetHandler(object scopeId, object actionId, params Type[] parameterTypes)
        {
            if (TryGetHandler(scopeId, actionId, parameterTypes, out var handler))
                return handler;

            throw new KeyNotFoundException($"Handler for action '{actionId}' not found. ScopeId: {scopeId}");
        }

        public bool TryGetHandler(object scopeId, object actionId, Type[] parameterTypes, out IHandler handlerItem)
        {
            if (handlers.TryGetValue(scopeId, out var hd))
            {
                if (hd.TryGetValue(actionId, out var handlerItems))
                {
                    foreach(var handler in handlerItems)
                    {
                        //if (handler.ParameterTypes.Length == parameterTypes.Length)
                        {
                            if (handlerItems.Count == 1)
                                handlerItem = handler;
                            else
                            {
                                bool isAssignable = true;
                                for(int i = 0; i < parameterTypes.Length; i++)
                                {
                                    if(!handler.ParameterTypes[i].GetTypeInfo()
                                        .IsAssignableFrom(parameterTypes[i]))
                                    {
                                        isAssignable = false;
                                        break;
                                    }
                                }

                                if (!isAssignable) continue;

                                handlerItem = handler;
                            }

                            return true;
                        }
                    }

                    
                }
            }

            handlerItem = null;
            return false;
        }

        public virtual void LoadFromReference<T>()
        {
            LoadFromReference(typeof(T));
        }

        public virtual void LoadFromReference(Type referenceType)
        {
            var types = Resolver.GetTypesFromReference<HandlerAttribute>(referenceType);
            foreach (var type in types)
            {
                var scopeAttr = type.GetTypeInfo().GetCustomAttribute<ScopeAttribute>(true);

                var classRoles = new List<RoleAttribute>();
                var methodRolles = new List<RoleAttribute>();

                var cr = type.GetTypeInfo().GetCustomAttributes<RoleAttribute>(true).ToArray();
                classRoles.AddRange(cr);

                var syncAttr = type.GetTypeInfo().GetCustomAttribute<SyncThreadAttribute>(true);
                var single = syncAttr != null ? true : false;
                var syncGroup = syncAttr?.SyncGroup;

                var methods = type.GetRuntimeMethods().ToArray();
                foreach (var method in methods)
                {
                    var methodInfo = method;

                    var mr = methodInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
                    methodRolles.AddRange(mr);

                    scopeAttr = methodInfo.GetCustomAttribute<ScopeAttribute>(true) ?? scopeAttr;

                    var actionAttr = methodInfo.GetCustomAttribute<ActionAttribute>(true);
                    if (actionAttr == null)
                    {
                        actionAttr = GetInterfaceAttribute<ActionAttribute>(type, ref methodInfo, out var interfaceType);
                        if (actionAttr == null)
                            continue;

                        if (scopeAttr == null)
                            scopeAttr = methodInfo.GetCustomAttribute<ScopeAttribute>(true);

                        if (scopeAttr == null)
                            scopeAttr = interfaceType.GetTypeInfo().GetCustomAttribute<ScopeAttribute>(true);

                        var ir = interfaceType.GetTypeInfo().GetCustomAttributes<RoleAttribute>(true).ToArray();
                        classRoles.AddRange(ir);

                        var imr = methodInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
                        methodRolles.AddRange(imr);
                    }

                    var mSyncAttr = methodInfo.GetCustomAttribute<SyncThreadAttribute>(true);
                    if (mSyncAttr != null)
                    {
                        single = true;
                        syncGroup = mSyncAttr.SyncGroup;
                    }

                    var parameters = methodInfo.GetParameters()
                        .Where(p =>
                        p.ParameterType.GetTypeInfo().IsValueType
                        ||
                        (
                            p.GetCustomAttribute<InjectAttribute>() == null
                            &&
                            p.ParameterType.GetTypeInfo().GetCustomAttribute<InjectAttribute>() == null
                        ))
                        .ToArray(); // todo ištestuoti su paveldėjimu

                    if(parameters.Length == 0)
                        throw new ArgumentException($"Method '{methodInfo.Name}' should contain parameter with ArgumentAttribute");

                    var handlerItem = CreateHandlerItem(type, methodInfo, parameters.Select(p => p.ParameterType).ToArray(), single, syncGroup, classRoles.ToArray(), methodRolles.ToArray());

                    var scopeId = scopeAttr?.GetScopeId() ?? "";

                    var actionId = actionAttr.GetActionId();
                    
                    if (handlers.TryGetValue(scopeId, out var hd))
                    {
                        if(hd.TryGetValue(actionId, out var handlerItems))
                            handlerItems.Add(handlerItem);
                        else
                        {
                            handlerItems = new List<IHandler>();
                            handlerItems.Add(handlerItem);
                            hd[actionId] = handlerItems;
                        }
                    }  
                    else
                    {
                        Dictionary<object, List<IHandler>> nhd = new Dictionary<object, List<IHandler>>();
                        var handlerItems = new List<IHandler>();
                        handlerItems.Add(handlerItem);
                        nhd[actionId] = handlerItems;
                        handlers[scopeId] = nhd;
                    }
                }
            }
        }

        public T GetInterfaceAttribute<T>(Type objType, ref MethodInfo methodInfo, out Type interfaceType) where T : Attribute
        {
            var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            foreach (var type in objType.GetTypeInfo().GetInterfaces())
            {
                var interfaceMethodInfo = type.GetRuntimeMethod(methodInfo.Name, paramTypes);
                if (interfaceMethodInfo != null)
                {
                    var attr = interfaceMethodInfo.GetCustomAttribute<T>(true);
                    if (attr != null)
                    {
                        interfaceType = type;
                        methodInfo = interfaceMethodInfo;
                        return attr;
                    }
                }
            }
            interfaceType = null;
            return null;
        }

        protected virtual IHandler CreateHandlerItem(Type handlerType, MethodInfo method, Type[] parameterTypes, bool singleThread, int? syncGroup, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
        {
            if(singleThread)
            {
                var slim = CreateSyncSlim(syncGroup);
                return new SThreadHandler(Resolver, handlerType, method, parameterTypes, slim, classRoles, methodRoles);
            }
            return new MThreadHandler(Resolver, handlerType, method, parameterTypes, classRoles, methodRoles);
        }

        protected virtual SemaphoreSlim CreateSyncSlim(int? syncGroup)
        {
            if (syncGroup.HasValue)
            {
                if (!slims.TryGetValue(syncGroup.Value, out var slim))
                {
                    slim = new SemaphoreSlim(1, 1);
                    slims[syncGroup.Value] = slim;
                }

                return slim;
            }
            return new SemaphoreSlim(1, 1);
        }
    }
}
