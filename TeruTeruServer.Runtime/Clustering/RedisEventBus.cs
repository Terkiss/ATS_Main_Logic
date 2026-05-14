using System;
using System.Collections.Concurrent;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// Redis Pub/Sub 기반 분산 이벤트 버스 시뮬레이션 구현체 (M12)
    /// </summary>
    public class RedisEventBus : IEventBus
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, Action<object>> _handlers = new();

        public RedisEventBus(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Publish<T>(string channel, T message)
        {
            // 로컬 핸들러 즉시 실행 (시뮬레이션)
            if (_handlers.TryGetValue(channel, out var handler))
            {
                handler(message!);
            }

            // TODO: Redis PUBLISH {channel} {json}
        }

        public void Subscribe<T>(string channel, Action<T> handler)
        {
            // TODO: Redis SUBSCRIBE {channel}
            _handlers[channel] = (msg) => handler((T)msg);
        }

        public void Unsubscribe(string channel)
        {
            // TODO: Redis UNSUBSCRIBE {channel}
            _handlers.TryRemove(channel, out _);
        }
    }
}
