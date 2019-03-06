using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public interface IHandlerProvider
    {
        bool TryGetHandler(object scopeId, object actionId, Type[] parameterTypes, out IHandler handler);

        IHandler GetHandler(object scopeId, object actionId, params Type[] parameterTypes);
    }
}
