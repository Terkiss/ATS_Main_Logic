using System;
using System.Collections.Concurrent;
using System.Text.Json;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// StackExchange.Redis의 Pub/Sub API를 사용하는 분산 이벤트 버스 구현체입니다.
    /// Redis 장애 시 로컬 핸들러를 호출하여 장애 전파를 방지합니다.
    /// </summary>
    public class RedisEventBus : IEventBus
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, Action<object>> _handlers = new();
        private readonly RedisConnectionProvider _provider;

        public RedisEventBus(string connectionString)
        {
            _connectionString = connectionString;
            _provider = RedisConnectionProvider.GetInstance(_connectionString);
        }

        public void Publish<T>(string channel, T message)
        {
            bool redisPublished = false;

            try
            {
                var sub = _provider.GetSubscriber();
                if (sub != null)
                {
                    string json = JsonSerializer.Serialize(message);
                    sub.Publish(channel, json);
                    redisPublished = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisEventBus] Publish failover triggered on channel '{channel}': {ex.Message}");
            }

            // Redis에 퍼블리시하지 못했거나 장애 상태인 경우 로컬 핸들러 즉시 실행 (Failover)
            if (!redisPublished)
            {
                TriggerLocalHandler(channel, message!);
            }
        }

        public void Subscribe<T>(string channel, Action<T> handler)
        {
            // 1. 로컬 핸들러 등록
            _handlers[channel] = (msg) => handler((T)msg);

            // 2. Redis 구독 등록
            try
            {
                var sub = _provider.GetSubscriber();
                if (sub != null)
                {
                    sub.Subscribe(channel, (redisChannel, value) =>
                    {
                        try
                        {
                            if (!value.IsNullOrEmpty)
                            {
                                var message = JsonSerializer.Deserialize<T>(value!);
                                if (message != null)
                                {
                                    handler(message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[RedisEventBus] Error handling message on channel '{channel}': {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisEventBus] Subscribe failed for channel '{channel}': {ex.Message}");
            }
        }

        public void Unsubscribe(string channel)
        {
            // 1. 로컬 핸들러 제거
            _handlers.TryRemove(channel, out _);

            // 2. Redis 구독 취소
            try
            {
                var sub = _provider.GetSubscriber();
                if (sub != null)
                {
                    sub.Unsubscribe(channel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisEventBus] Unsubscribe failed for channel '{channel}': {ex.Message}");
            }
        }

        private void TriggerLocalHandler<T>(string channel, T message)
        {
            if (_handlers.TryGetValue(channel, out var handler))
            {
                try
                {
                    handler(message!);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RedisEventBus] Local handler execution failed: {ex.Message}");
                }
            }
        }
    }
}
