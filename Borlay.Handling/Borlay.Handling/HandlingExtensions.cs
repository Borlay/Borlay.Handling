﻿using Borlay.Arrays;
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
        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session, 
            string scopeId, int actionId, object[] requests, CancellationToken cancellationToken)
        {
            var parameterTypes = requests.Where(r => !(r is CancellationToken)).Select(r => r.GetType()).ToArray();
            var returnType = typeof(T);
            var parameterHash = TypeHasher.GetMethodBytes(parameterTypes, returnType);

            var scopeBytes = Encoding.UTF8.GetBytes(scopeId);
            var actionBytes = new byte[4];
            actionBytes.AddBytes<int>(actionId, 4, 0);

            var actionHash = TypeHasher.CreateMD5Hash(scopeBytes, actionBytes, parameterHash);

            var handler = handlerProvider.GetHandler(actionHash.ToByteArray());
            return (T)await handler.HandleAsync(session, requests, cancellationToken);
        }



        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            int actionId, params object[] requests)
        {
            return await HandleAsync<T>(handlerProvider, session, "", actionId, requests, CancellationToken.None);
        }

        public static async Task<T> HandleAsync<T>(this IHandlerProvider handlerProvider, IResolverSession session,
            string scopeId, int actionId, params object[] requests)
        {
            return await HandleAsync<T>(handlerProvider, session, scopeId, actionId, requests, CancellationToken.None);
        }
    }
}
