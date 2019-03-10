using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class NameScopeAttribute : ScopeAttribute
    {
        private readonly byte[] nameBytes;
        private readonly ByteArray byteArray;

        public string ScopeName { get; }

        public NameScopeAttribute(string scopeName)
        {
            if (string.IsNullOrWhiteSpace(scopeName))
                throw new ArgumentNullException(nameof(scopeName));

            this.ScopeName = scopeName.ToLower();

            nameBytes = Encoding.UTF8.GetBytes(this.ScopeName);
            if (nameBytes.Length > Byte.MaxValue)
                throw new ArgumentException("Method name is too long");

            this.byteArray = new ByteArray(nameBytes);
        }

        public override object GetScopeId()
        {
            return this.byteArray;
        }
    }
}
