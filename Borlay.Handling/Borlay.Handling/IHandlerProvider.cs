using Borlay.Arrays;
using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public interface IHandlerProvider
    {
        bool TryGetHandler(object scopeId, object actionId, ByteArray methodHash, out IHandler handler);

        IHandler GetHandler(object scopeId, object actionId, ByteArray methodHash);
    }
}
