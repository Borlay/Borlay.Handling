using Borlay.Arrays;
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
        TypeContext GetMethodContext(Type type);
    }

    public interface IMethodContextInfoProvider
    {
        MethodContextInfo[] GetMethodContextInfo(Type type);
    }

    public class MethodContextProvider : ITypeContextProvider, IMethodContextInfoProvider
    {
        protected readonly ConcurrentDictionary<Type, TypeContext> contexts = new ConcurrentDictionary<Type, TypeContext>();

        public virtual MethodContextInfo[] GetMethodContextInfo(Type type)
        {
            return GetMethodContext(type).Methods.Select(t => t.ContextInfo).ToArray();
        }

        public virtual TypeContext GetMethodContext(Type type)
        {
            if (contexts.TryGetValue(type, out var context))
                return context;

            var methods = CreateMethodContext(type);
            context = new TypeContext(methods);
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
                var mb = TypeHasher.GetMethodBytes(pts, g.ReturnType);
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
                var parameterHash = TypeHasher.GetMethodBytes(parameterTypes.ToArray(), returnType);
                
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

                    var scopeId = ResolveScopeId(type, method, scopeAttr);
                    var actionId = ResolveActionId(type, method, actionAttr);

                    var actionHash = TypeHasher.CreateMD5Hash(scopeId, actionId, parameterHash);

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

        protected virtual byte[] ResolveScopeId(Type type, MethodInfo methodInfo, ScopeAttribute scopeAttr)
        {
            return scopeAttr?.GetScopeId() ?? Encoding.UTF8.GetBytes(type.Name);
        }

        protected virtual byte[] ResolveActionId(Type type, MethodInfo methodInfo, ActionAttribute actionAttr)
        {
            return actionAttr?.GetActionId() ?? Encoding.UTF8.GetBytes(methodInfo.Name);
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
        protected readonly Dictionary<string, Dictionary<ByteArray, MethodContext>> dictionary = new Dictionary<string, Dictionary<ByteArray, MethodContext>>();

        public MethodContext[] Methods { get; }

        public TypeContext(params MethodContext[] methodContexts)
        {
            this.Methods = methodContexts;

            var groups = methodContexts.GroupBy(m => m.ContextInfo.MethodInfo.Name);
            foreach (var g in groups)
            {
                var dict = g.ToDictionary(m => m.ContextInfo.ActionHash);
                dictionary.Add(g.Key, dict);
            }
        }

        public virtual MethodContext GetContext(string methodName, ByteArray parameterHash)
        {
            if (!dictionary.TryGetValue(methodName, out var methodMetadatas))
                throw new KeyNotFoundException($"Method for name '{methodName}' not found");

            if (!methodMetadatas.TryGetValue(parameterHash, out var metaData))
                throw new KeyNotFoundException($"Method for name '{methodName}' not found");

            return metaData;
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
