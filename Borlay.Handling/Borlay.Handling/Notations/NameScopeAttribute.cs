using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    public class NameScopeAttribute : ScopeAttribute
    {
        private readonly byte[] nameBytes;

        public string ScopeName { get; }

        public NameScopeAttribute(string scopeName)
        {
            if (scopeName == null)
                throw new ArgumentNullException(nameof(scopeName));

            this.ScopeName = scopeName.ToLower();

            nameBytes = Encoding.UTF8.GetBytes(this.ScopeName);
            if (nameBytes.Length > Byte.MaxValue)
                throw new ArgumentException("Method name is too long");
        }

        public override byte[] GetScopeId()
        {
            return nameBytes;
        }
    }
}
