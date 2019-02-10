//using Borlay.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling
{
    //public interface IQueueHandling
    //{
    //    IEnqueue<TValue> GetEnqueue<TValue>();

    //    IEnqueue<TValue> GetEnqueue<TValue>(string action);
    //}

    //public class QueueHandling : IQueueHandling
    //{
    //    private readonly HandlerProvider handlerProvider;

    //    Dictionary<Type, Dictionary<string, object>> queues = new Dictionary<Type, Dictionary<string, object>>();

    //    public QueueHandling(HandlerProvider handlerProvider)
    //    {

    //    }

    //    public IEnqueue<TValue> GetEnqueue<TValue>()
    //    {
    //        return GetQueue<TValue>("");
    //    }

    //    public IEnqueue<TValue> GetEnqueue<TValue>(string action)
    //    {
    //        return GetQueue<TValue>(action);
    //    }

    //    private AsyncQueue<TValue> GetQueue<TValue>()
    //    {
    //        return GetQueue<TValue>("");
    //    }

    //    private AsyncQueue<TValue> GetQueue<TValue>(string action)
    //    {
    //        if (queues.TryGetValue(typeof(TValue), out var actions))
    //        {
    //            if(actions.TryGetValue(action, out var value))
    //                return (AsyncQueue<TValue>)value;
    //            else
    //            {
    //                var queue = new AsyncQueue<TValue>();
    //                actions[action] = queue;
    //                return queue;
    //            }
    //        } 
    //        else
    //        {
    //            var dict = new Dictionary<string, object>();
    //            queues[typeof(TValue)] = dict;
    //            var queue = new AsyncQueue<TValue>();
    //            dict[action] = queue;
    //            return queue;
    //        }
    //    }

    //    public async Task RunQueue<TValue>(string actionId, CancellationToken cancellationToken)
    //    {
    //        var queue = GetQueue<TValue>(actionId);

    //        do
    //        {
    //            var obj = await queue.DequeueAsync(cancellationToken);
    //            var handler = handlerProvider.GetHandler(actionId, typeof(TValue));
    //            await handler.HandleAsync(obj, cancellationToken);
    //        }
    //        while (cancellationToken.IsCancellationRequested);
    //    }
    //}
}
