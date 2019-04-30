using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public class ScopeAttribute : Attribute, IGetId
    {
        private readonly byte[] nameBytes;

        public string ScopeName { get; }

        public ScopeAttribute(string scopeName)
        {
            if (scopeName == null)
                throw new ArgumentNullException(nameof(scopeName));

            this.ScopeName = scopeName.ToLower();

            nameBytes = Encoding.UTF8.GetBytes(this.ScopeName);
            if (nameBytes.Length > Byte.MaxValue)
                throw new ArgumentException("Method name is too long");
        }

        public string GetId()
        {
            return ScopeName;
        }
    }
}
