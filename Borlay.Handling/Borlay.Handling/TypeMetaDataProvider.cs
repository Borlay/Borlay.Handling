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
            List<MethodContext> metas = new List<MethodContext>();

            var typeInfo = type.GetTypeInfo();

            var classScopeAttr = typeInfo.GetCustomAttribute<ScopeAttribute>(true);
            var cr = typeInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
            var syncAttr = typeInfo.GetCustomAttribute<SyncThreadAttribute>(true);

            //var methods = type.GetRuntimeMethods().OrderBy(m => m.GetParameters().Length).ToArray();

            var methods = type.GetInterfacesMethods()
                .Where(m => m.GetCustomAttribute<ActionAttribute>(true) != null).Distinct().ToArray();

            foreach (var method in methods)
            {
                var methodInfo = method;

                var classRoles = new List<RoleAttribute>();
                var methodRoles = new List<RoleAttribute>();
                classRoles.AddRange(cr);

                var single = syncAttr != null ? true : false;
                var syncGroup = syncAttr?.SyncGroup;

                var mr = methodInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
                methodRoles.AddRange(mr);

                var scopeAttr = methodInfo.DeclaringType.GetTypeInfo().GetCustomAttribute<ScopeAttribute>(true) ?? classScopeAttr;

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
                    methodRoles.AddRange(imr);
                }

                var scopeId = ResolveScopeId(type, scopeAttr?.GetScopeId());
                var actionId = actionAttr.GetActionId();

                var mSyncAttr = methodInfo.GetCustomAttribute<SyncThreadAttribute>(true);
                if (mSyncAttr != null)
                {
                    single = true;
                    syncGroup = mSyncAttr.SyncGroup;
                }

                var parameterTypes = new List<Type>();
                var argumentIndexes = new List<int>();
                var ctIndex = -1;

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
                var methodHash = TypeHasher.GetMethodHash(parameterTypes.ToArray(), returnType);


                var tcsType = typeof(TaskCompletionSource<>);
                var retType = returnType.GenericTypeArguments.FirstOrDefault() ?? typeof(bool);
                var tcsGenType = tcsType.MakeGenericType(retType);

                var context = new MethodContextInfo()
                {
                    MethodInfo = methodInfo,
                    ClassRoles = classRoles.ToArray(),
                    MethodRoles = methodRoles.ToArray(),
                    IsSync = single,
                    SyncGroup = syncGroup,
                    ScopeId = scopeId,
                    ActionId = actionId,
                    ParameterHash = methodHash,
                };


                var meta = new MethodContext()
                {
                    ContextInfo = context,
                    TaskCompletionSourceType = tcsGenType,
                    ArgumentIndexes = argumentIndexes.ToArray(),
                    CancellationIndex = ctIndex,
                };

                metas.Add(meta);
            }

            return metas.ToArray();
        }

        protected virtual object ResolveScopeId(Type type, object scopeId)
        {
            return scopeId ?? "";
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
                var dict = g.ToDictionary(m => m.ContextInfo.ParameterHash);
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

        public object ScopeId { get; set; }
        public object ActionId { get; set; }
        public ByteArray ParameterHash { get; set; }
    }

    public class MethodContext
    {
       public MethodContextInfo ContextInfo { get; set; }

        public int[] ArgumentIndexes { get; set; }

        public int CancellationIndex { get; set; }

        public Type TaskCompletionSourceType { get; set; }
    }
}
