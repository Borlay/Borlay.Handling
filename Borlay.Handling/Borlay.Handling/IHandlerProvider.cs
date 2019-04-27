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
        bool TryGetHandler(ByteArray actionHash, out IHandler handler);

        IHandler GetHandler(ByteArray actionHash);
    }
}
