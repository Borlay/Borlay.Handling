using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling.Notations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SyncThreadAttribute : Attribute
    {
        public int? SyncGroup { get; set; }
    }
}
