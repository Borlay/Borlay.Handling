using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionAttribute : Attribute, IGetId
    {
        private readonly byte[] nameBytes;

        public string MethodName { get; }

        public ActionAttribute([CallerMemberName] string methodName = "")
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException(nameof(methodName));

            this.MethodName = methodName.ToLower();

            nameBytes = Encoding.UTF8.GetBytes(this.MethodName);
            if (nameBytes.Length > Byte.MaxValue)
                throw new ArgumentException("Method name is too long");
        }

        public string GetId()
        {
            return MethodName; //MethodName;
        }
    }

    public interface IGetId
    {
        string GetId();
    }
}
