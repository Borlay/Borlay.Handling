using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class ActionAttribute : Attribute, IActionMeta
    {
        public bool CanBeCached { get; set; } = false;

        public bool CacheReceivedResponse { get; set; } = false;

        public bool CacheSendedResponse { get; set; } = false;

        public abstract byte[] GetActionId();
    }
}
