using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public class ScopeAttribute : Attribute, IGetId
    {
        public string ScopeName { get; }

        public ScopeAttribute(string scopeName)
        {
            if (scopeName == null)
                throw new ArgumentNullException(nameof(scopeName));

            this.ScopeName = scopeName.ToLower();
        }

        public ScopeAttribute(int id)
        {
            this.ScopeName = id.ToString();
        }

        public string GetId()
        {
            return ScopeName;
        }
    }
}
