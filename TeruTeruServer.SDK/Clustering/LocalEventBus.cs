using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.SDK.Clustering
{
    /// <summary>
    /// 단일 프로세스 내에서 동작하는 단순한 이벤트 버스 구현체입니다.
    /// </summary>
    public class LocalEventBus : IEventBus
    {
        private readonly ConcurrentDictionary<string, List<Delegate>> _subscriptions = new ConcurrentDictionary<string, List<Delegate>>();

        public void Publish<T>(string channel, T message)
        {
            if (_subscriptions.TryGetValue(channel, out var handlers))
            {
                lock (handlers)
                {
                    foreach (var handler in handlers)
                    {
                        if (handler is Action<T> typedHandler)
                        {
                            typedHandler.Invoke(message);
                        }
                    }
                }
            }
        }

        public void Subscribe<T>(string channel, Action<T> handler)
        {
            var handlers = _subscriptions.GetOrAdd(channel, _ => new List<Delegate>());
            lock (handlers)
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe(string channel)
        {
            _subscriptions.TryRemove(channel, out _);
        }
    }
}
