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
        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session, 
            object actionId, object[] requests, CancellationToken cancellationToken)
        {
            var parameterTypes = requests.Where(r => !(r is CancellationToken)).Select(r => r.GetType()).ToArray();
            var returnType = typeof(T);
            var methodHash = TypeHasher.GetMethodHash(parameterTypes, returnType);
            var handler = handlerProvider.GetHandler("", actionId, methodHash);
            return (T)await handler.HandleAsync(session, requests, cancellationToken);
        }

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            object actionId, object request)
        {
            var returnType = typeof(T);
            var methodHash = TypeHasher.GetMethodHash(new Type[] { request.GetType() }, returnType);

            var handler = handlerProvider.GetHandler("", actionId, methodHash);
            return (T)await handler.HandleAsync(session, new object[] { request }, CancellationToken.None);
        }

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            object scopeId, object actionId, object request)
        {
            var returnType = typeof(T);
            var methodHash = TypeHasher.GetMethodHash(new Type[] { request.GetType() }, returnType);

            var handler = handlerProvider.GetHandler(scopeId, actionId, methodHash);
            return (T)await handler.HandleAsync(session, new object[] { request }, CancellationToken.None);
        }

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            object actionId, object[] requests)
        {
            var parameterTypes = requests.Select(r => r.GetType()).ToArray();
            var returnType = typeof(T);
            var methodHash = TypeHasher.GetMethodHash(parameterTypes, returnType);

            var handler = handlerProvider.GetHandler("", actionId, methodHash);
            return (T)await handler.HandleAsync(session, requests, CancellationToken.None);
        }
    }
}
