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

        public SThreadHandler(IResolver resolver, Type handlerType, MethodInfo method, SemaphoreSlim slim, RoleAttribute[] classRoles, RoleAttribute[] methodRoles)
            : base(resolver, handlerType, method, classRoles, methodRoles)
        {
            this.slim = slim;
        }

        public override async Task<object> HandleAsync(IResolver resolver, object request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await slim.WaitAsync();
            try
            {
                return await base.HandleAsync(resolver, request, cancellationToken);
            }
            finally
            {
                slim.Release();
            }
        }
    }
}
