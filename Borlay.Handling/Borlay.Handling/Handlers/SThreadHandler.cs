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

        public SThreadHandler(IResolver resolver, Type handlerType, MethodInfo method, Type[] parameterTypes, SemaphoreSlim slim, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
            : base(resolver, handlerType, method, parameterTypes, classRoles, methodRoles)
        {
            this.slim = slim;
        }

        public override async Task<object> HandleAsync(IResolver resolver, object[] requests, CancellationToken cancellationToken)
        {
            await slim.WaitAsync();
            try
            {
                return await base.HandleAsync(resolver, requests, cancellationToken);
            }
            finally
            {
                slim.Release();
            }
        }
    }
}
