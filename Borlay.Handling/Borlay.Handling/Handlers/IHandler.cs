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
        Task<object> HandleAsync(IResolverSession session, object[] requests, CancellationToken cancellationToken);
    }
}
