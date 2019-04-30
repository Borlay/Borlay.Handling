using Borlay.Arrays;
using Borlay.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    public static class HandlingExtensions
    {
        //public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session, 
        //    string scopeId, string actionId, object[] requests, CancellationToken cancellationToken)
        //{
        //    var actionBytes = new byte[4];
        //    actionBytes.AddBytes<int>(actionId, 4, 0);

        //    return await HandleAsync<T>(handlerProvider, session, scopeId, actionBytes, requests, cancellationToken);
        //}

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            string scopeId, string actionId, object[] requests, CancellationToken cancellationToken)
        {
            var parameterTypes = requests.Where(r => !(r is CancellationToken)).Select(r => r.GetType()).ToArray();
            var returnType = typeof(T);
            var parameterHash = TypeHasher.GetMethodBytes(parameterTypes, returnType, t => t.Name);

            //var scopeBytes = Encoding.UTF8.GetBytes(scopeId);
            var actionHash = TypeHasher.CreateMD5Hash(Encoding.UTF8.GetBytes($"scope-{scopeId}:action-{actionId}"), parameterHash);

            var handler = handlerProvider.GetHandler(actionHash.ToByteArray());
            return (T)await handler.HandleAsync(session, requests, cancellationToken);
        }



        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            int actionId, params object[] requests)
        {
            return await HandleAsync<T>(handlerProvider, session, "", actionId.ToString(), requests, CancellationToken.None);
        }

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            string scopeId, int actionId, params object[] requests)
        {
            return await HandleAsync<T>(handlerProvider, session, scopeId, actionId.ToString(), requests, CancellationToken.None);
        }

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            string scopeId, string actionId, params object[] requests)
        {
            //var actionBytes = Encoding.UTF8.GetBytes(actionId);
            return await HandleAsync<T>(handlerProvider, session, scopeId, actionId, requests, CancellationToken.None);
        }

    }
}
