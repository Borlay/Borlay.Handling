using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public static class HandlingExtensions
    {
        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, 
            object actionId, object[] requests, CancellationToken cancellationToken)
        {
            var handler = handlerProvider.GetHandler("", actionId, requests.Select(r => r.GetType()).ToArray());
            return await handler.HandleAsync(requests, cancellationToken);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider,
            object actionId, object request)
        {
            var handler = handlerProvider.GetHandler("", actionId, new Type[] { request.GetType() });
            return await handler.HandleAsync(new object[] { request }, CancellationToken.None);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider,
            object scopeId, object actionId, object request)
        {
            var handler = handlerProvider.GetHandler(scopeId, actionId, new Type[] { request.GetType() });
            return await handler.HandleAsync(new object[] { request }, CancellationToken.None);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider,
            object actionId, object[] requests)
        {
            var handler = handlerProvider.GetHandler("", actionId, requests.Select(r => r.GetType()).ToArray());
            return await handler.HandleAsync(requests, CancellationToken.None);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider,
            object scopeId, object actionId, object[] requests)
        {
            var handler = handlerProvider.GetHandler(scopeId, actionId, requests.Select(r => r.GetType()).ToArray());
            return await handler.HandleAsync(requests, CancellationToken.None);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, IResolver resolver,
            object actionId, object[] requests, CancellationToken cancellationToken)
        {
            var handler = handlerProvider.GetHandler("", actionId, requests.Select(r => r.GetType()).ToArray());
            return await handler.HandleAsync(resolver, requests, cancellationToken);
        }

        public static async Task<object> HandleAsync(this IHandlerProvider handlerProvider, IResolver resolver,
            object scopeId, object actionId, object[] requests, CancellationToken cancellationToken)
        {
            var handler = handlerProvider.GetHandler(scopeId, actionId, requests.Select(r => r.GetType()).ToArray());
            return await handler.HandleAsync(resolver, requests, cancellationToken);
        }
    }
}
