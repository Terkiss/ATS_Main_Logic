using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// Redis 기반 세션 저장소 시뮬레이션 구현체 (M12)
    /// </summary>
    public class RedisSessionStore : ISessionStore
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<int, ClientSession> _localCache = new();

        public RedisSessionStore(string connectionString)
        {
            _connectionString = connectionString;
            // TODO: StackExchange.Redis ConnectionMultiplier 초기화 로직
        }

        public bool TryAdd(int hostId, ClientSession session)
        {
            // TODO: Redis SET session:{hostId} json
            return _localCache.TryAdd(hostId, session);
        }

        public bool TryGet(int hostId, out ClientSession session)
        {
            // 로컬 캐시 우선
            if (_localCache.TryGetValue(hostId, out session!))
            {
                return true;
            }

            // TODO: Redis GET session:{hostId}
            return false;
        }

        public bool TryRemove(int hostId, out ClientSession session)
        {
            // TODO: Redis DEL session:{hostId}
            return _localCache.TryRemove(hostId, out session!);
        }

        public ClientSession? FindByReconnectToken(string token)
        {
            // TODO: Redis SCAN or Secondary Index 조회
            return _localCache.Values.FirstOrDefault(s => s.ReconnectToken == token);
        }

        public IEnumerable<ClientSession> GetAll()
        {
            // TODO: Redis KEYS session:* or SMEMBERS
            return _localCache.Values;
        }
    }
}
