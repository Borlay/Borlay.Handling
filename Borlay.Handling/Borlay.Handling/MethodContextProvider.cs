﻿using Borlay.Arrays;
using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public interface ITypeContextProvider
    {
        TypeContext GetTypeContext(Type type);

        IEnumerable<TypeContext> GetTypeContexts();
    }

    public interface IMethodContextInfoProvider
    {
        IEnumerable<MethodContextInfo> GetMethodContextInfo(Type type);

        IEnumerable<MethodContextInfo> GetMethodContextInfo();
    }

    [Resolve(IncludeBase = true, Singletone = true)]
    public class MethodContextProvider : ITypeContextProvider, IMethodContextInfoProvider
    {
        protected readonly ConcurrentDictionary<Type, TypeContext> contexts = new ConcurrentDictionary<Type, TypeContext>();

        protected static MethodContextProvider current = new MethodContextProvider();
        public static MethodContextProvider Current
        {
            get => current;
            set => current = value;
        }

        public virtual IEnumerable<MethodContextInfo> GetMethodContextInfo(Type type)
        {
            return GetTypeContext(type).Methods.Select(t => t.ContextInfo);
        }

        public virtual IEnumerable<MethodContextInfo> GetMethodContextInfo()
        {
            return GetTypeContexts().SelectMany(c => c.Methods.Select(t => t.ContextInfo));
        }

        public IEnumerable<TypeContext> GetTypeContexts()
        {
            return contexts.Select(c => c.Value);
        }

        public virtual TypeContext GetTypeContext(Type type)
        {
            if (contexts.TryGetValue(type, out var context))
                return context;

            var methods = CreateMethodContext(type);
            context = new TypeContext(type, methods);
            contexts[type] = context;
            return context;
        }

        protected virtual MethodContext[] CreateMethodContext(Type type)
        {
            Dictionary<ByteArray, MethodContext> contexts = new Dictionary<ByteArray, MethodContext>();

            var typeInfo = type.GetTypeInfo();

            var classScopeAttr = typeInfo.GetCustomAttribute<ScopeAttribute>(true);
            var cr = typeInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
            var syncAttr = typeInfo.GetCustomAttribute<SyncThreadAttribute>(true);

            var methodGroups = type.GetInterfacesMethods().GroupBy(g =>
            {
                var pts = g.GetParameters().Select(t => t.ParameterType).ToArray();
                var mb = TypeHasher.GetMethodBytes(pts, g.ReturnType, t => t.Name);
                var mn = Encoding.UTF8.GetBytes(g.Name);
                var bytes = TypeHasher.CreateMD5Hash(mb, mn);
                return new ByteArray(bytes);
            });

            var single = syncAttr != null ? true : false;
            var syncGroup = syncAttr?.SyncGroup;

            foreach (var methods in methodGroups)
            {
                var classRoles = new List<RoleAttribute>();
                var methodRoles = new List<RoleAttribute>();
                classRoles.AddRange(cr);
                
                var mr = methods.SelectMany(m => m.GetCustomAttributes<RoleAttribute>(true)).ToArray();
                methodRoles.AddRange(mr);

                var mcr = methods.SelectMany(m => m.DeclaringType.GetTypeInfo().GetCustomAttributes<RoleAttribute>(true)).ToArray();
                classRoles.AddRange(mcr);

                var mSyncAttr = methods.SelectMany(m => m.GetCustomAttributes<SyncThreadAttribute>(true)).FirstOrDefault();
                if (mSyncAttr != null)
                {
                    single = true;
                    syncGroup = mSyncAttr.SyncGroup;
                }

                var parameterTypes = new List<Type>();
                var argumentIndexes = new List<int>();
                var ctIndex = -1;

                var methodInfo = methods.First();

                var mp = methodInfo.GetParameters();
                for (int i = 0; i < mp.Length; i++)
                {
                    var paramInfo = mp[i];

                    if (paramInfo.ParameterType == typeof(CancellationToken))
                        ctIndex = i;

                    if (paramInfo.NeedSkip()) continue;

                    parameterTypes.Add(paramInfo.ParameterType);
                    argumentIndexes.Add(i);
                }

                var returnType = methodInfo.ReturnType;
                var parameterHash = TypeHasher.GetMethodBytes(parameterTypes.ToArray(), returnType, ResolveTypeName);
                
                var tcsType = typeof(TaskCompletionSource<>);
                var retType = returnType.GenericTypeArguments.FirstOrDefault() ?? typeof(bool);
                var tcsGenType = tcsType.MakeGenericType(retType);

                foreach (var method in methods)
                {
                    if (method.DeclaringType == typeof(object)) continue;

                    var scopeAttr = method.DeclaringType.GetTypeInfo().GetCustomAttribute<ScopeAttribute>(true);
                    var methodScopeAttr = method.GetCustomAttribute<ScopeAttribute>(true);
                    if (methodScopeAttr != null)
                        scopeAttr = methodScopeAttr;

                    var actionAttr = method.GetCustomAttribute<ActionAttribute>(true);

                    var scopeId = ResolveScope(type, method, scopeAttr);
                    var actionId = ResolveAction(type, method, actionAttr);

                    var actionHash = TypeHasher.CreateMD5Hash(Encoding.UTF8.GetBytes($"scope-{scopeId}:action-{actionId}"), parameterHash);

                    var contextInfo = new MethodContextInfo()
                    {
                        MethodInfo = methodInfo,
                        ClassRoles = classRoles.ToArray(),
                        MethodRoles = methodRoles.ToArray(),
                        IsSync = single,
                        SyncGroup = syncGroup,
                        ActionHash = new ByteArray(actionHash),
                    };


                    var context = new MethodContext()
                    {
                        ContextInfo = contextInfo,
                        TaskCompletionSourceType = tcsGenType,
                        ArgumentIndexes = argumentIndexes.ToArray(),
                        CancellationIndex = ctIndex,
                    };

                    contexts[contextInfo.ActionHash] = context;
                }
            }

            return contexts.Select(m => m.Value).ToArray();
        }

        protected virtual string ResolveTypeName(Type type)
        {
            return type.Name;
        }

        protected virtual string ResolveScope(Type type, MethodInfo methodInfo, ScopeAttribute scopeAttr)
        {
            return scopeAttr?.GetId() ?? methodInfo.DeclaringType.Name;
        }

        protected virtual string ResolveAction(Type type, MethodInfo methodInfo, ActionAttribute actionAttr)
        {
            return actionAttr?.GetId() ?? methodInfo.Name;
        }

        protected virtual T GetInterfaceAttribute<T>(Type objType, ref MethodInfo methodInfo, out Type interfaceType) where T : Attribute
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
    }

    public class TypeContext
    {
        protected readonly Dictionary<ByteArray, MethodContext> contexts = new Dictionary<ByteArray, MethodContext>();

        public MethodContext[] Methods { get; }

        public Type Type { get; }

        public TypeContext(Type type, params MethodContext[] methodContexts)
        {
            this.Type = type;
            this.Methods = methodContexts;
            contexts = methodContexts.ToDictionary(m => m.ContextInfo.ActionHash);
        }

        public virtual MethodContext GetMethodContext(ByteArray actionHash)
        {
            if (contexts.TryGetValue(actionHash, out var metaData))
                return metaData;

            throw new KeyNotFoundException($"Method for given action hash not found");
        }

        public virtual bool TryeGetMethodContext(ByteArray actionHash, out MethodContext methodContext)
        {
            if (contexts.TryGetValue(actionHash, out methodContext))
                return true;

            methodContext = null;
            return true;
        }
    }

    public class MethodContextInfo
    {
        public MethodInfo MethodInfo { get; set; }

        public RoleAttribute[] ClassRoles { get; set; }
        public RoleAttribute[] MethodRoles { get; set; }

        public bool IsSync { get; set; }
        public int? SyncGroup { get; set; }

        public ByteArray ActionHash { get; set; }
    }

    public class MethodContext
    {
       public MethodContextInfo ContextInfo { get; set; }

        public int[] ArgumentIndexes { get; set; }

        public int CancellationIndex { get; set; }

        public Type TaskCompletionSourceType { get; set; }
    }
}
