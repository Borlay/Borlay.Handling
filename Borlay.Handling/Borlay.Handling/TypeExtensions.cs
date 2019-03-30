using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Borlay.Handling
{
    public static class TypeExtensions
    {
        public static IEnumerable<MethodInfo> GetInterfacesMethods(this Type type)
        {
            foreach (var method in type.GetTypeInfo().GetMethods())
                yield return method;

            foreach(var it in type.GetTypeInfo().ImplementedInterfaces)
            {
                foreach (var method in GetInterfacesMethods(it))
                    yield return method;
            }
        }
    }
}
