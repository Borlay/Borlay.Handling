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
        Dictionary<Type, Dictionary<string, IHandler>> handlers = new Dictionary<Type, Dictionary<string, IHandler>>();
        Dictionary<int, SemaphoreSlim> slims = new Dictionary<int, SemaphoreSlim>();
        Dictionary<byte, IActionConverter> actionSolves = new Dictionary<byte, IActionConverter>();

        public Resolver Resolver { get; private set; }

        public HandlerProvider()
        {
            this.Resolver = new Resolver();
        }

        public HandlerProvider(IResolver Resolver)
        {
            this.Resolver = new Resolver(Resolver);
        }

        public IActionConverter GetActionConverter(byte type)
        {
            if (actionSolves.TryGetValue(type, out var actionIdSolve))
                return actionIdSolve;

            throw new KeyNotFoundException($"Action converter for action type '{type}' not found");
        }

        public bool TryGetActionConverter(byte type, out IActionConverter actionConverter)
        {
            return actionSolves.TryGetValue(type, out actionConverter);
        }

        public IHandler GetHandler(string actionId, params Type[] types)
        {
            if (TryGetHandler(actionId, types, out var handler))
                return handler;

            throw new KeyNotFoundException($"Handler for action '{actionId}' not found");
        }

        public bool TryGetHandler(string actionId, Type[] types, out IHandler handlerItem)
        {
            if (types.Length == 1)
            {
                if (handlers.TryGetValue(types.First(), out var hd))
                {
                    if (hd.TryGetValue(actionId, out handlerItem))
                        return true;
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
            foreach(var type in types)
            {
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

                    var actionAttr = methodInfo.GetCustomAttribute<ActionAttribute>(true);
                    if (actionAttr == null)
                    {
                        methodInfo = GetInterfaceMethod(type, methodInfo, out var interfaceType);
                        if (methodInfo == null)
                            continue;

                        actionAttr = methodInfo.GetCustomAttribute<ActionAttribute>(true);
                        if (actionAttr == null)
                            continue;

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

                    var handlerItem = CreateHandlerItem(type, methodInfo, single, syncGroup, classRoles.ToArray(), methodRolles.ToArray());
                    Type argType = null;

                    var parameter = methodInfo.GetParameters()
                        .FirstOrDefault(p => p.GetCustomAttribute<ArgumentAttribute>() != null); // todo ištestuoti su paveldėjimu
                    if (parameter != null)
                    {
                        argType = parameter.ParameterType;
                    }
                    else
                    {
                        parameter = methodInfo.GetParameters()
                            .FirstOrDefault(p => p.ParameterType.GetTypeInfo()
                            .GetCustomAttribute<ArgumentAttribute>() != null);

                        if (parameter == null)
                            throw new ArgumentException($"Method '{methodInfo.Name}' should contain parameter with ArgumentAttribute");

                        argType = parameter.ParameterType;
                    }
                    if (!actionSolves.ContainsKey(actionAttr.ActionType))
                        actionSolves[actionAttr.ActionType] = actionAttr;

                    var actionId = actionAttr.GetActionId();

                    if (handlers.TryGetValue(argType, out var hd))
                        hd[actionId] = handlerItem;
                    else
                    {
                        Dictionary<string, IHandler> nhd = new Dictionary<string, IHandler>();
                        nhd[actionId] = handlerItem;
                        handlers[argType] = nhd;
                    }
                }
            }
        }

        public MethodInfo GetInterfaceMethod(Type objType, MethodInfo methodInfo, out Type interfaceType)
        {
            var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            foreach (var type in objType.GetTypeInfo().GetInterfaces())
            {
                var interfaceMethodInfo = type.GetRuntimeMethod(methodInfo.Name, paramTypes);
                if (interfaceMethodInfo != null)
                {
                    if(interfaceMethodInfo.GetCustomAttribute<ActionAttribute>(true) != null)
                    {
                        interfaceType = type;
                        return interfaceMethodInfo;
                    }
                }
            }
            interfaceType = null;
            return null;
        }

        protected virtual IHandler CreateHandlerItem(Type handlerType, MethodInfo method, bool singleThread, int? syncGroup, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
        {
            if(singleThread)
            {
                var slim = CreateSyncSlim(syncGroup);
                return new SThreadHandler(Resolver, handlerType, method, slim, classRoles, methodRoles);
            }
            return new MThreadHandler(Resolver, handlerType, method, classRoles, methodRoles);
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
