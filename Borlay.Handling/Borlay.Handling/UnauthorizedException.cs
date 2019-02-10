using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedReason UnauthorizedReason { get; }

        public UnauthorizedException(UnauthorizedReason unauthorizedReason)
            : base($"Unauthorized access to method. Reason: '{unauthorizedReason.ToString()}'")
        {
            this.UnauthorizedReason = unauthorizedReason;
        }
    }

    public enum UnauthorizedReason
    {
        NoRoleProvider = 1,
        ClassAccess = 2,
        MethodAccess = 3
    }
}
