using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class RoleAttribute : Attribute
    {
        public string[] AnyOfRoles { get; }

        /// <summary>
        /// User should have at least one role to execute.
        /// </summary>
        /// <param name="anyOfRoles">Should have at least one role</param>
        public RoleAttribute(params string[] anyOfRoles)
        {
            this.AnyOfRoles = anyOfRoles;
        }
    }
}
