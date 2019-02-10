using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class RoleAttribute : Attribute
    {
        public string[] Roles { get; }

        /// <summary>
        /// User should have roles to execute.
        /// </summary>
        /// <param name="roles">Should have all roles</param>
        public RoleAttribute(params string[] roles)
        {
            this.Roles = roles;
        }
    }
}
