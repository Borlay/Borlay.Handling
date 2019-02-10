using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public static class HandlingExtensions
    {
        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, 
            int actionId, object request, CancellationToken cancellationToken)
        {
            return await handlerProvider.HandleAsync(actionId.ToString(), request, cancellationToken);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, IResolver resolver, 
            int actionId, object request, CancellationToken cancellationToken)
        {
            return await handlerProvider.HandleAsync(resolver, actionId.ToString(), request, cancellationToken);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, 
            string actionId, object request, CancellationToken cancellationToken)
        {
            var handler = handlerProvider.GetHandler(actionId, request.GetType());
            return await handler.HandleAsync(request, cancellationToken);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, IResolver resolver, 
            string actionId, object request, CancellationToken cancellationToken)
        {
            var handler = handlerProvider.GetHandler(actionId, request.GetType());
            return await handler.HandleAsync(resolver, request, cancellationToken);
        }
    }
}
