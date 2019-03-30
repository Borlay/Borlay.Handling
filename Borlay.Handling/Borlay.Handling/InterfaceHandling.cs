using Borlay.Arrays;
using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public static class InterfaceHandling
    {
        //public static TInterface CreateHandler<TInterface, THandler>(this IResolver resolver) where TInterface : class where THandler : IInterfaceHandler
        //{
        //    var typeInfo = CreateTypeInfo(typeof(TInterface), typeof(THandler));
        //    var obj = (TInterface)Resolver.CreateInstance(resolver, typeInfo);
        //    return obj;
        //}

        public static TInterface CreateHandler<TInterface, THandler>(this IResolverSession session) where TInterface : class where THandler : IInterfaceHandler
        {
            var typeInfo = CreateTypeInfo(typeof(TInterface), typeof(THandler));
            var obj = (TInterface)session.CreateInstance(typeInfo);
            return obj;
        }

        public static TInterface CreateHandler<TInterface, THandler>(params object[] arguments) where TInterface : class where THandler : IInterfaceHandler
        {
            var typeInfo = CreateTypeInfo(typeof(TInterface), typeof(THandler));

            var paramTypes = arguments.Select(p => p.GetType()).ToArray();
            var constructorInfo = typeInfo.GetConstructors()
                .FirstOrDefault(c => 
                    c.GetParameters().Length == paramTypes.Length 
                    && 
                    c.GetParameters().All(p => paramTypes.Contains(p.ParameterType)));

            if (constructorInfo == null)
                throw new KeyNotFoundException($"Constructor for type '{typeof(THandler).Name}' with given arguments not found");

            var obj = (TInterface)constructorInfo.Invoke(arguments);
            return obj;
        }

        public static TypeInfo CreateTypeInfo(Type interfaceType, Type handlerType)
        {
            var assemblyName = new Guid().ToString();

            var handleAsyncMethod = handlerType.GetRuntimeMethod("HandleAsync", 
                new Type[] { typeof(string), typeof(byte[]), typeof(object[]) });

            var htInfo = handleAsyncMethod.ReturnType.GetTypeInfo();

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType($"{handlerType.Name}_handler_for_{interfaceType.Name}", TypeAttributes.Public, handlerType);

            var baseConstructors = handlerType.GetTypeInfo().GetConstructors();

            foreach (var baseConstr in baseConstructors)
            {
                var parameters = baseConstr.GetParameters();
                var constrBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                    parameters.Select(p => p.ParameterType).ToArray());
                

                var cil = constrBuilder.GetILGenerator();
                cil.Emit(OpCodes.Ldarg_0);

                for(int i = 0; i < parameters.Length; i++)
                {
                    cil.Emit(OpCodes.Ldarg, i + 1);
                }

                cil.Emit(OpCodes.Call, baseConstr);
                cil.Emit(OpCodes.Ret);
            }

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var methods = interfaceType.GetInterfacesMethods().Distinct().ToArray();
            foreach (var methodInfo in methods)
            {
                var parameters = methodInfo.GetParameters();

                var mtInfo = methodInfo.ReturnType.GetTypeInfo();

                var hashParameterTypes = parameters.SkipIncluded().Select(p => p.ParameterType).ToArray();
                var methodHash = TypeHasher.GetMethodHash(hashParameterTypes, methodInfo.ReturnType);
                
                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, 
                    methodInfo.ReturnType, parameters.Select(p => p.ParameterType).ToArray());

                var il = methodBuilder.GetILGenerator();

                if (methodInfo.IsSpecialName)
                {
                    il.ThrowException(typeof(NotImplementedException));
                    continue;
                }

                if (!(methodInfo.ReturnType == typeof(void)) && mtInfo.IsValueType != htInfo.IsValueType)
                    throw new Exception($"Handler and interface return types should be both value type or not value type. Method name '{methodInfo.Name}'");

                if (typeof(Task).GetTypeInfo().IsAssignableFrom(htInfo) && !typeof(Task).GetTypeInfo().IsAssignableFrom(mtInfo))
                    throw new Exception($"Interfeice return type should inherit Task. Method name '{methodInfo.Name}'");

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, methodInfo.Name);
                il.AddArray(methodHash.Bytes);

                il.Emit(OpCodes.Ldc_I4, parameters.Length);
                il.Emit(OpCodes.Newarr, typeof(object));

                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg, i + 1);

                    var ptype = parameters[i].ParameterType;
                    if (ptype.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, ptype);

                    il.Emit(OpCodes.Stelem_Ref);
                }

                il.EmitCall(OpCodes.Callvirt, handleAsyncMethod, null);

                if (methodInfo.ReturnType == typeof(void))
                    il.Emit(OpCodes.Pop);

                il.Emit(OpCodes.Ret);
            }

            var typeInfo = typeBuilder.CreateTypeInfo();
            return typeInfo;
        }

        public static void AddArray(this ILGenerator il, byte[] arr)
        {
            var ptype = typeof(byte);

            il.Emit(OpCodes.Ldc_I4, arr.Length);
            il.Emit(OpCodes.Newarr, ptype);

            var iarr = new int[(arr.Length / 4) + (arr.Length % 4 > 0 ? 1 : 0)];
            Buffer.BlockCopy(arr, 0, iarr, 0, arr.Length);

            //il.Emit(OpCodes.Ldloc, arr);

            for (int i = 0; i < iarr.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldc_I4, iarr[i]);
                il.Emit(OpCodes.Stelem_I4);


                //il.Emit(OpCodes.Dup);
                //il.Emit(OpCodes.Ldc_I4, i);
                //il.Emit(OpCodes.Ldarg, i + 1);

                ////var ptype = parameters[i].ParameterType;
                ////if (ptype.GetTypeInfo().IsValueType)
                //il.Emit(OpCodes.Box, ptype);

                //il.Emit(OpCodes.Stelem_Ref);
            }
        }

        public static IntPtr EmitInst<TInst>(this ILGenerator il, TInst inst) where TInst : class
        {
            var gch = GCHandle.Alloc(inst);
            var ptr = GCHandle.ToIntPtr(gch);
            
            if (IntPtr.Size == 4)
                il.Emit(OpCodes.Ldc_I4, ptr.ToInt32());
            else
                il.Emit(OpCodes.Ldc_I8, ptr.ToInt64());

            il.Emit(OpCodes.Ldobj, typeof(TInst));

            return ptr;
            /// Do this only if you can otherwise ensure that 'inst' outlives the DynamicMethod
            //gch.Free();
        }


    }

    public interface IInterfaceHandler
    {
        object HandleAsync(string methodName, byte[] methodHash, object[] args);
    }
}
