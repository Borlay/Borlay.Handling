using Borlay.Handling.Notations;
using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public class SThreadHandler : MThreadHandler
    {
        private readonly SemaphoreSlim slim;

        public SThreadHandler(Type handlerType, MethodInfo method, SemaphoreSlim slim, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
            : base(handlerType, method, classRoles, methodRoles)
        {
            this.slim = slim;
        }

        public override async Task<object> HandleAsync(IResolverSession session, object[] requests, CancellationToken cancellationToken)
        {
            await slim.WaitAsync();
            try
            {
                return await base.HandleAsync(session, requests, cancellationToken);
            }
            finally
            {
                slim.Release();
            }
        }
    }
}
