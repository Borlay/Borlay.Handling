using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class ActionAttribute : Attribute, IActionSolve
    {
        public bool CanBeCached { get; set; } = false;

        public bool CacheReceivedResponse { get; set; } = false;

        public bool CacheSendedResponse { get; set; } = false;

        public abstract string GetActionId();

        public abstract byte ActionType { get; }

        public abstract void AddActionId(byte[] bytes, ref int index);

        public abstract string GetActionId(byte[] bytes, ref int index);
    }
}
