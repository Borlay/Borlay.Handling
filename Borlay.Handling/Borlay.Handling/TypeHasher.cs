using Borlay.Arrays;
using Borlay.Handling.Notations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public static class TypeHasher
    {
        public static IEnumerable<ParameterInfo> SkipIncluded(this IEnumerable<ParameterInfo> parameterInfos)
        {
            foreach (var par in parameterInfos)
            {
                var inja =
                    par.GetCustomAttribute<InjectAttribute>()
                    ??
                    par.ParameterType.GetTypeInfo().GetCustomAttribute<InjectAttribute>();

                if (inja != null) continue;

                yield return par;
            }
        }

        public static string GetTypeName<T>(bool ignoreTask)
        {
            return GetTypeName(ignoreTask, typeof(T));
        }

        public static string GetMethodString(Type[] parameterTypes, Type returnType)
        {
            var argumentHash = TypeHasher.GetTypeName(false, parameterTypes);
            argumentHash = $"{argumentHash}:{TypeHasher.GetTypeName(true, returnType)}";
            return argumentHash;
        }

        public static ByteArray GetMethodHash(Type[] parameterTypes, Type returnType)
        {
            var argumentHash = GetMethodString(parameterTypes, returnType);
            var hash = CreateMD5Hash(argumentHash);
            return hash;
        }

        public static string GetTypeName(bool ignoreTask, params Type[] types)
        {
            var sb = new StringBuilder();
            BuildTypeName(sb, ignoreTask, types);
            if (sb.Length == 0)
                sb.Append("Void");
            return sb.ToString();
        }

        public static void BuildTypeName(StringBuilder sb, bool ignoreTask, params Type[] types)
        {
            foreach (var type in types)
            {
                if (!(ignoreTask && typeof(Task).GetTypeInfo().IsAssignableFrom(type)))
                    sb.Append(type.Name);

                if (type.GenericTypeArguments != null && type.GenericTypeArguments.Length > 0)
                {
                    BuildTypeName(sb, ignoreTask, type.GenericTypeArguments);
                }
                if (type.HasElementType)
                {
                    BuildTypeName(sb, ignoreTask, type.GetElementType());
                }
            }
        }

        public static ByteArray CreateMD5Hash(string value)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                return new ByteArray(data);
            }
        }
    }
}
