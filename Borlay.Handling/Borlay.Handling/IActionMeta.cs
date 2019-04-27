using Borlay.Arrays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Handling
{
    public interface IActionMeta
    {
        bool CanBeCached { get; }

        bool CacheReceivedResponse { get; }

        bool CacheSendedResponse { get; }

        byte[] GetActionId();
    }
}
