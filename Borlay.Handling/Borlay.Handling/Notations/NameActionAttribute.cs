using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class NameActionAttribute : ActionAttribute
    {
        private readonly byte[] nameBytes;

        public string MethodName { get; }

        public NameActionAttribute([CallerMemberName] string methodName = "")
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException(nameof(methodName));

            this.MethodName = methodName.ToLower();

            nameBytes = Encoding.UTF8.GetBytes(this.MethodName);
            if (nameBytes.Length > Byte.MaxValue)
                throw new ArgumentException("Method name is too long");
        }

        public override byte[] GetActionId()
        {
            return nameBytes; //MethodName;
        }
    }
}
