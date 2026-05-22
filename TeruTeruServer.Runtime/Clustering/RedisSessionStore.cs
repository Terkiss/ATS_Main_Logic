using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// StackExchange.Redis를 사용하는 실전 분산 세션 저장소 구현체입니다.
    /// Redis 장애 시 로컬 백업 캐시로 Failover를 수행합니다.
    /// </summary>
    public class RedisSessionStore : ISessionStore
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<int, ClientSession> _localCache = new();
        private readonly RedisConnectionProvider _provider;

        public RedisSessionStore(string connectionString)
        {
            _connectionString = connectionString;
            _provider = RedisConnectionProvider.GetInstance(_connectionString);
        }

        public bool TryAdd(int hostId, ClientSession session)
        {
            // 1. 로컬 캐시에 먼저 등록 시도
            bool localAdded = _localCache.TryAdd(hostId, session);
            if (!localAdded) return false;

            // 2. Redis에 동기화 시도
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    var dto = RedisSessionData.FromSession(session);
                    string json = JsonSerializer.Serialize(dto);

                    // Redis Key: session:{hostId}
                    db.StringSet($"session:{hostId}", json, TimeSpan.FromHours(24));

                    // Secondary Index: reconnect:{token} -> hostId
                    if (!string.IsNullOrEmpty(session.ReconnectToken))
                    {
                        db.StringSet($"reconnect:{session.ReconnectToken}", hostId, TimeSpan.FromHours(24));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisSessionStore] TryAdd Failover triggered: {ex.Message}");
            }

            return true;
        }

        public bool TryGet(int hostId, out ClientSession session)
        {
            // 1. 로컬 캐시 확인
            if (_localCache.TryGetValue(hostId, out session!))
            {
                return true;
            }

            // 2. Redis에서 확인 및 로컬 캐시 채우기
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    string? json = db.StringGet($"session:{hostId}");
                    if (!string.IsNullOrEmpty(json))
                    {
                        var dto = JsonSerializer.Deserialize<RedisSessionData>(json);
                        if (dto != null)
                        {
                            session = dto.ToSession();
                            _localCache[hostId] = session;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisSessionStore] TryGet Failover triggered: {ex.Message}");
            }

            return false;
        }

        public bool TryRemove(int hostId, out ClientSession session)
        {
            // 1. 로컬 캐시에서 제거 시도
            bool localRemoved = _localCache.TryRemove(hostId, out session!);

            // 2. Redis에서 동기 제거 시도
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    // Secondary Index 제거용 reconnect token 획득
                    string? reconnectToken = session?.ReconnectToken;
                    if (string.IsNullOrEmpty(reconnectToken))
                    {
                        // 로컬 캐시에 세션이 없었던 경우, Redis에서 직접 세션을 조회해 토큰을 확인합니다.
                        string? json = db.StringGet($"session:{hostId}");
                        if (!string.IsNullOrEmpty(json))
                        {
                            var dto = JsonSerializer.Deserialize<RedisSessionData>(json);
                            reconnectToken = dto?.ReconnectToken;
                        }
                    }

                    db.KeyDelete($"session:{hostId}");

                    if (!string.IsNullOrEmpty(reconnectToken))
                    {
                        db.KeyDelete($"reconnect:{reconnectToken}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisSessionStore] TryRemove Failover triggered: {ex.Message}");
            }

            return localRemoved;
        }

        public ClientSession? FindByReconnectToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            // 1. Redis Secondary Index에서 hostId 검색
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    string? hostIdStr = db.StringGet($"reconnect:{token}");
                    if (int.TryParse(hostIdStr, out int hostId))
                    {
                        if (TryGet(hostId, out var session))
                        {
                            return session;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisSessionStore] FindByReconnectToken Failover triggered: {ex.Message}");
            }

            // 2. Redis 장애 혹은 인덱스 유실 시 로컬 캐시 순회 검색 (Fallback)
            return _localCache.Values.FirstOrDefault(s => s.ReconnectToken == token);
        }

        public IEnumerable<ClientSession> GetAll()
        {
            // Redis의 실시간 세션 전체를 긁어와 로컬 캐시를 최신화하고 반환을 시도합니다.
            try
            {
                var conn = _provider.Connection;
                var db = _provider.GetDatabase();
                if (conn != null && db != null)
                {
                    var endpoints = conn.GetEndPoints();
                    var list = new List<ClientSession>();
                    
                    foreach (var endpoint in endpoints)
                    {
                        var server = conn.GetServer(endpoint);
                        if (server.IsConnected)
                        {
                            var keys = server.Keys(pattern: "session:*");
                            foreach (var key in keys)
                            {
                                string? json = db.StringGet(key);
                                if (!string.IsNullOrEmpty(json))
                                {
                                    var dto = JsonSerializer.Deserialize<RedisSessionData>(json);
                                    if (dto != null)
                                    {
                                        var session = dto.ToSession();
                                        _localCache[dto.HostID] = session;
                                        list.Add(session);
                                    }
                                }
                            }
                        }
                    }
                    
                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisSessionStore] GetAll Failover triggered: {ex.Message}");
            }

            // Failover: 로컬 캐시 데이터 반환
            return _localCache.Values;
        }
    }
}
