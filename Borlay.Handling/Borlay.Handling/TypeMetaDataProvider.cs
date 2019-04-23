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
    public static class TypeMetaDataProvider
    {
        private readonly static ConcurrentDictionary<Type, TypeMetaData> typeMetaDatas = new ConcurrentDictionary<Type, TypeMetaData>();

        public static TypeMetaData GetTypeMetaData<T>()
        {
            var type = typeof(T);
            return GetTypeMetaData(type);
        }

        public static TypeMetaData GetTypeMetaData(Type type)
        {
            if (typeMetaDatas.TryGetValue(type, out var typeMetaData))
                return typeMetaData;

            typeMetaData = new TypeMetaData(type);
            typeMetaDatas[type] = typeMetaData;
            return typeMetaData;
        }

    }

    public class TypeMetaData
    {
        protected readonly Dictionary<string, Dictionary<ByteArray, MethodMetadata>> methods = new Dictionary<string, Dictionary<ByteArray, MethodMetadata>>();
        protected readonly List<MethodMetadata> metadatas = new List<MethodMetadata>();

        public MethodMetadata[] Metadatas => metadatas.ToArray();

        public TypeMetaData(Type type)
        {
            var methodGroups = type.GetInterfacesMethods().Distinct()
                .Where(m => m.GetCustomAttribute<ActionAttribute>(true) != null).GroupBy(m => m.Name);

            var classScopeAttr = type.GetTypeInfo().GetCustomAttribute<ScopeAttribute>(true);

            foreach (var g in methodGroups)
            {
                var methodMeta = g.Select(m =>
                {
                    if (!typeof(Task).GetTypeInfo().IsAssignableFrom(m.ReturnType))
                        throw new ArgumentNullException($"Method '{m.Name}' return type should be Task based.");

                    var parameters = m.GetParameters();
                    var ptypes = parameters.Select(p => p.ParameterType).ToArray();
                    var actionAttr = m.GetCustomAttribute<ActionAttribute>(true);
                    var methodScopeAttr = m.GetCustomAttribute<ScopeAttribute>(true) ?? classScopeAttr;

                    //var index = -1;
                    var ctIndex = -1;

                    var argumentIndexes = new List<int>();
                    var argumentTypes = new List<Type>();

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        var ptype = param.ParameterType;
                        var typeInfo = param.ParameterType.GetTypeInfo();
                        if (
                        param.GetCustomAttribute<InjectAttribute>(true) == null
                            &&
                            typeInfo.GetCustomAttribute<InjectAttribute>(true) == null
                            &&
                            param.ParameterType != typeof(CancellationToken)
                            &&
                            !typeof(IResolver).GetTypeInfo().IsAssignableFrom(param.ParameterType)
                        )
                        {
                            argumentIndexes.Add(i);
                            argumentTypes.Add(ptype);
                        }

                        if (ptype == typeof(CancellationToken))
                            ctIndex = i;
                    }

                    var methodHash = TypeHasher.GetMethodHash(argumentTypes.ToArray(), m.ReturnType);

                    var tcsType = typeof(TaskCompletionSource<>);
                    var retType = m.ReturnType.GenericTypeArguments.FirstOrDefault() ?? typeof(bool);
                    var tcsGenType = tcsType.MakeGenericType(retType);

                    var meta = new MethodMetadata()
                    {
                        ArgumentIndexes = argumentIndexes.ToArray(),
                        CancellationIndex = ctIndex,
                        ReturnType = m.ReturnType,
                        ActionId = actionAttr?.GetActionId(),
                        ScopeId = methodScopeAttr?.GetScopeId(),
                        MethodHash = methodHash,
                        TaskCompletionSourceType = tcsGenType
                    };
                    return meta;
                }).ToArray();

                metadatas.AddRange(methodMeta);

                var dict = methodMeta.ToDictionary(m => m.MethodHash);
                methods.Add(g.Key, dict);
            }
        }

        public MethodMetadata[] CreateMetaData(Type type)
        {
            List<MethodMetadata> metas = new List<MethodMetadata>();

            var typeInfo = type.GetTypeInfo();

            var classScopeAttr = typeInfo.GetCustomAttribute<ScopeAttribute>(true);
            var cr = typeInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
            var syncAttr = typeInfo.GetCustomAttribute<SyncThreadAttribute>(true);

            var methods = type.GetRuntimeMethods().OrderBy(m => m.GetParameters().Length).ToArray();
            foreach (var method in methods)
            {
                var classRoles = new List<RoleAttribute>();
                var methodRoles = new List<RoleAttribute>();
                classRoles.AddRange(cr);

                var single = syncAttr != null ? true : false;
                var syncGroup = syncAttr?.SyncGroup;

                var methodInfo = method;

                var mr = methodInfo.GetCustomAttributes<RoleAttribute>(true).ToArray();
                methodRoles.AddRange(mr);

                var scopeAttr = methodInfo.GetCustomAttribute<ScopeAttribute>(true) ?? classScopeAttr;

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

                var mSyncAttr = methodInfo.GetCustomAttribute<SyncThreadAttribute>(true);
                if (mSyncAttr != null)
                {
                    single = true;
                    syncGroup = mSyncAttr.SyncGroup;
                }

                var parameterTypes = new List<Type>();
                var argumentIndexes = new List<int>();

                var mp = methodInfo.GetParameters();
                for (int i = 0; i< mp.Length; i++)
                {
                    var paramInfo = mp[i];
                    if (paramInfo.NeedSkip()) continue;

                    parameterTypes.Add(paramInfo.ParameterType);
                    argumentIndexes.Add(i);
                }

                var returnType = methodInfo.ReturnType;
                var methodHash = TypeHasher.GetMethodHash(parameterTypes.ToArray(), returnType);

                var scopeId = scopeAttr?.GetScopeId() ?? type.Name;
                var actionId = actionAttr.GetActionId() ?? "";



                var tcsType = typeof(TaskCompletionSource<>);
                var retType = returnType.GenericTypeArguments.FirstOrDefault() ?? typeof(bool);
                var tcsGenType = tcsType.MakeGenericType(retType);

                var meta = new MethodMetadata()
                {
                    Type = type,
                    MethodInfo = methodInfo,
                    ClassRoles = classRoles.ToArray(),
                    MethodRoles = methodRoles.ToArray(),
                    IsSync = single,
                    SyncGroup = syncGroup,
                    ScopeId = scopeId,
                    ActionId = actionId,
                    MethodHash = methodHash,
                    ReturnType = returnType,
                    TaskCompletionSourceType = tcsGenType,

                };

                //var handlerItem = CreateHandlerItem(type, methodInfo, single, syncGroup, classRoles.ToArray(), methodRolles.ToArray());

                //if (handlers.TryGetValue(scopeId, out var hd))
                //{
                //    if (hd.TryGetValue(actionId, out var handlerItems))
                //        handlerItems[methodHash] = handlerItem;
                //    else
                //    {
                //        handlerItems = new Dictionary<ByteArray, IHandler>();
                //        handlerItems[methodHash] = handlerItem;
                //        hd[actionId] = handlerItems;
                //    }
                //}
                //else
                //{
                //    Dictionary<object, Dictionary<ByteArray, IHandler>> nhd = new Dictionary<object, Dictionary<ByteArray, IHandler>>();
                //    var handlerItems = new Dictionary<ByteArray, IHandler>();
                //    handlerItems[methodHash] = handlerItem;
                //    nhd[actionId] = handlerItems;
                //    handlers[scopeId] = nhd;
                //}
            }

            return metas.ToArray();
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

        public MethodMetadata GetMetaData(string methodName, ByteArray methodHash)
        {
            if (!methods.TryGetValue(methodName, out var methodMetadatas))
                throw new KeyNotFoundException($"Method for name '{methodName}' not found");

            if (!methodMetadatas.TryGetValue(methodHash, out var metaData))
                throw new KeyNotFoundException($"Method for name '{methodName}' not found");

            return metaData;
        }

    }

    public class MethodMetadata
    {
        public Type Type { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public RoleAttribute[] ClassRoles { get; set; }

        public RoleAttribute[] MethodRoles { get; set; }

        public bool IsSync { get; set; }

        public int? SyncGroup { get; set; }

        public ByteArray MethodHash { get; set; }

        public object ActionId { get; set; }

        public object ScopeId { get; set; }

        public int[] ArgumentIndexes { get; set; }

        public int CancellationIndex { get; set; }

        public Type ReturnType { get; set; }

        public Type TaskCompletionSourceType { get; set; }
    }
}
