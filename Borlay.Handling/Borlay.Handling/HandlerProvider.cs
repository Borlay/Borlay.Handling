using Borlay.Arrays;
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
        protected readonly Dictionary<object, Dictionary<object, Dictionary<ByteArray, IHandler>>> handlers = 
            new Dictionary<object, Dictionary<object, Dictionary<ByteArray, IHandler>>>();

        protected readonly Dictionary<int, SemaphoreSlim> slims = 
            new Dictionary<int, SemaphoreSlim>();

        protected readonly IMethodContextInfoProvider methodContextInfoProvider;

        public HandlerProvider()
            : this(new MethodContextProvider())
        {

        }

        public HandlerProvider(IMethodContextInfoProvider methodContextInfoProvider)
        {
            if (methodContextInfoProvider == null)
                throw new ArgumentNullException(nameof(methodContextInfoProvider));

            this.methodContextInfoProvider = methodContextInfoProvider;
        }


        public IHandler GetHandler(object scopeId, object actionId, ByteArray methodHash)
        {
            if (TryGetHandler(scopeId, actionId, methodHash, out var handler))
                return handler;

            throw new KeyNotFoundException($"Handler for action '{actionId}' not found. ScopeId: {scopeId}");
        }

        public bool TryGetHandler(object scopeId, object actionId, ByteArray methodHash, out IHandler handlerItem)
        {
            if (handlers.TryGetValue(scopeId, out var hd))
            {
                if (hd.TryGetValue(actionId, out var handlerItems))
                {
                    if (handlerItems.TryGetValue(methodHash, out handlerItem))
                        return true;
                }
            }

            handlerItem = null;
            return false;
        }

        public virtual int LoadFromReference<T>()
        {
            return LoadFromReference(typeof(T));
        }

        public virtual int LoadFromReference(Type referenceType)
        {
            return LoadFromReference(referenceType, methodContextInfoProvider);
        }

        public virtual int LoadFromReference(Type referenceType, IMethodContextInfoProvider methodContextInfoProvider)
        {
            var count = 0;
            var types = Resolver.GetTypesFromReference<HandlerAttribute>(referenceType);
            foreach (var type in types)
            {
                if (RegisterHandler(type, methodContextInfoProvider))
                    count++;
            }
            return count;
        }

        public bool RegisterHandler(Type type)
        {
            return RegisterHandler(type, methodContextInfoProvider);
        }

        public bool RegisterHandler(Type type, IMethodContextInfoProvider methodContextInfoProvider)
        {
            var methods = methodContextInfoProvider.GetMethodContextInfo(type);
            return RegisterHandler(type, methods);
        }

        public bool RegisterHandler(Type type, params MethodContextInfo[] methods)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsAbstract || typeInfo.IsGenericTypeDefinition)
                return false;

            if (methods == null || methods.Length == 0) return false;

            foreach(var method in methods)
            {
                var handlerItem = CreateHandlerItem(type, method.MethodInfo, method.IsSync,
                    method.SyncGroup, method.ClassRoles, method.MethodRoles);

                if (handlers.TryGetValue(method.ScopeId, out var hd))
                {
                    if (hd.TryGetValue(method.ActionId, out var handlerItems))
                        handlerItems[method.ParameterHash] = handlerItem;
                    else
                    {
                        handlerItems = new Dictionary<ByteArray, IHandler>();
                        handlerItems[method.ParameterHash] = handlerItem;
                        hd[method.ActionId] = handlerItems;
                    }
                }
                else
                {
                    Dictionary<object, Dictionary<ByteArray, IHandler>> nhd = new Dictionary<object, Dictionary<ByteArray, IHandler>>();
                    var handlerItems = new Dictionary<ByteArray, IHandler>();
                    handlerItems[method.ParameterHash] = handlerItem;
                    nhd[method.ActionId] = handlerItems;
                    handlers[method.ScopeId] = nhd;
                }
            }

            return true;
        }
        

        protected virtual IHandler CreateHandlerItem(Type handlerType, MethodInfo method, bool singleThread, int? syncGroup, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
        {
            if(singleThread)
            {
                var slim = CreateSyncSlim(syncGroup);
                return new SThreadHandler(handlerType, method, slim, classRoles, methodRoles);
            }
            return new MThreadHandler(handlerType, method, classRoles, methodRoles);
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
