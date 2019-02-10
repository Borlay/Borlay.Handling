using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public interface IHandlerProvider : IActionConverterProvider
    {
        bool TryGetHandler(string actionId, Type[] types, out IHandler handler);

        IHandler GetHandler(string actionId, params Type[] types);
    }
}
