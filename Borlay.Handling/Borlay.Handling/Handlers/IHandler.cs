using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public interface IHandler
    {
        IActionMeta ActionMeta { get; }
        Type[] ParameterTypes { get; }

        Task<object> HandleAsync(object[] requests, CancellationToken cancellationToken);
        Task<object> HandleAsync(IResolver resolver, object[] requests, CancellationToken cancellationToken);
    }
}
