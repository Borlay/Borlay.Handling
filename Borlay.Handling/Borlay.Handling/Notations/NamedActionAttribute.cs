using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class NamedActionAttribute : ActionAttribute
    {
        private readonly byte[] nameBytes;

        public string MethodName { get; }
        public override byte ActionType => 2;

        public NamedActionAttribute([CallerMemberName] string methodName = "")
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException(nameof(methodName));

            this.MethodName = methodName.ToLower();

            nameBytes = Encoding.UTF8.GetBytes(this.MethodName);
            if (nameBytes.Length > Byte.MaxValue)
                throw new ArgumentException("Method name is too long");
        }

        public override string GetActionId()
        {
            return MethodName;
        }

        public override void AddActionId(byte[] bytes, ref int index)
        {
            bytes[index++] = (byte)nameBytes.Length;
            Array.Copy(nameBytes, 0, bytes, index, nameBytes.Length);
            index += nameBytes.Length;
        }

        public override string GetActionId(byte[] bytes, ref int index)
        {
            var count = bytes[index++];
            var id = Encoding.UTF8.GetString(bytes, index, count);
            index += count;
            return id;
        }
    }
}
