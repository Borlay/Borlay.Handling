﻿using Borlay.Arrays;
using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public static class TypeHasher
    {
        public static IEnumerable<ParameterInfo> SkipIncluded(this IEnumerable<ParameterInfo> parameterInfos)
        {
            foreach (var par in parameterInfos)
            {
                if (par.NeedSkip()) continue;
                yield return par;
            }
        }

        public static bool NeedSkip(this ParameterInfo info)
        {
            var inja =
                    info.GetCustomAttribute<InjectAttribute>()
                    ??
                    info.ParameterType.GetTypeInfo().GetCustomAttribute<InjectAttribute>();

            if (inja != null) return true;

            if (info.ParameterType == typeof(CancellationToken) ||
                typeof(IResolver).GetTypeInfo().IsAssignableFrom(info.ParameterType))
                return true;

            return false;
        }

        public static string GetTypeName<T>(bool ignoreTask, Func<Type, string> nameResolver)
        {
            return GetTypeName(ignoreTask, nameResolver, typeof(T));
        }

        public static string GetMethodString(Type[] parameterTypes, Type returnType)
        {
            return GetMethodString(parameterTypes, returnType, t => t.Name);
        }

        public static string GetMethodString(Type[] parameterTypes, Type returnType, Func<Type, string> nameResolver)
        {
            var argumentHash = TypeHasher.GetTypeName(false, nameResolver, parameterTypes);
            argumentHash = $"{argumentHash}:{TypeHasher.GetTypeName(true, nameResolver, returnType)}";
            return argumentHash;
        }

        public static byte[] GetMethodBytes(Type[] parameterTypes, Type returnType, Func<Type, string> nameResolver)
        {
            var argumentHash = GetMethodString(parameterTypes, returnType, nameResolver);
            var bytes = Encoding.UTF8.GetBytes(argumentHash);
            return bytes;
        }

        public static string GetTypeName(bool ignoreTask, Func<Type, string> nameResolver, params Type[] types)
        {
            var sb = new StringBuilder();
            BuildTypeName(sb, ignoreTask, nameResolver, types);
            if (sb.Length == 0)
                sb.Append("Void");
            return sb.ToString();
        }

        public static void BuildTypeName(StringBuilder sb, bool ignoreTask, Func<Type, string> nameResolver, params Type[] types)
        {
            foreach (var type in types)
            {
                if (!(ignoreTask && typeof(Task).GetTypeInfo().IsAssignableFrom(type)))
                    sb.Append(nameResolver(type));

                if (type.GenericTypeArguments != null && type.GenericTypeArguments.Length > 0)
                {
                    BuildTypeName(sb, ignoreTask, nameResolver, type.GenericTypeArguments);
                }
                if (type.HasElementType)
                {
                    BuildTypeName(sb, ignoreTask, nameResolver, type.GetElementType());
                }
            }
        }

        public static byte[] CreateMD5Hash(params byte[][] bytesArray)
        {
            var length = bytesArray.Sum(b => b.Length);
            var bytes = new byte[length];
            var index = 0;
            foreach(var b in bytesArray)
            {
                Buffer.BlockCopy(b, 0, bytes, index, b.Length);
                index += b.Length;
            }

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(bytes);
                return data;
            }
        }
    }
}
