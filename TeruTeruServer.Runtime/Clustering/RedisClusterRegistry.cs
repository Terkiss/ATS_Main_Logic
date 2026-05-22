using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// Redis Hash 구조를 사용하여 클러스터 노드 정보를 저장하고 조회하는 레지스트리 구현체입니다.
    /// Redis 연결 장애 시 로컬 백업 ConcurrentDictionary를 통해 무중단으로 동작합니다.
    /// </summary>
    public class RedisClusterRegistry : IClusterRegistry
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, ClusterNodeInfo> _nodes = new();
        private readonly RedisConnectionProvider _provider;
        private const string RedisHashKey = "nodes";

        public RedisClusterRegistry(string connectionString)
        {
            _connectionString = connectionString;
            _provider = RedisConnectionProvider.GetInstance(_connectionString);
        }

        public void RegisterNode(ClusterNodeInfo node)
        {
            // 1. 로컬 캐시 갱신
            _nodes[node.NodeId] = node;

            // 2. Redis Hash에 등록
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    string json = JsonSerializer.Serialize(node);
                    db.HashSet(RedisHashKey, node.NodeId, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisClusterRegistry] RegisterNode Failover triggered for '{node.NodeId}': {ex.Message}");
            }
        }

        public void DeregisterNode(string nodeId)
        {
            // 1. 로컬 캐시에서 제거
            _nodes.TryRemove(nodeId, out _);

            // 2. Redis Hash에서 제거
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    db.HashDelete(RedisHashKey, nodeId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisClusterRegistry] DeregisterNode Failover triggered for '{nodeId}': {ex.Message}");
            }
        }

        public ClusterNodeInfo? GetNode(string nodeId)
        {
            // 1. Redis에서 직접 데이터 확보 시도
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    string? json = db.HashGet(RedisHashKey, nodeId);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var node = JsonSerializer.Deserialize<ClusterNodeInfo>(json);
                        if (node != null)
                        {
                            _nodes[nodeId] = node;
                            return node;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RedisClusterRegistry] GetNode Failover triggered for '{nodeId}': {ex.Message}");
            }

            // Fallback: 로컬 캐시 반환
            return _nodes.TryGetValue(nodeId, out var cachedNode) ? cachedNode : null;
        }

        public IReadOnlyList<ClusterNodeInfo> GetActiveNodes()
        {
            // 1. Redis에서 전체 노드 목록 조회 시도
            try
            {
                var db = _provider.GetDatabase();
                if (db != null)
                {
                    var entries = db.HashGetAll(RedisHashKey);
                    var list = new List<ClusterNodeInfo>();
                    foreach (var entry in entries)
                    {
                        if (!entry.Value.IsNullOrEmpty)
                        {
                            var node = JsonSerializer.Deserialize<ClusterNodeInfo>(entry.Value!);
                            if (node != null)
                            {
                                _nodes[node.NodeId] = node;
                                list.Add(node);
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
                Console.WriteLine($"[RedisClusterRegistry] GetActiveNodes Failover triggered: {ex.Message}");
            }

            // Fallback: 로컬 캐시 노드 목록 반환
            return _nodes.Values.ToList();
        }

        public void UpdateHeartbeat(string nodeId)
        {
            // 1. 로컬 캐시 업데이트
            if (_nodes.TryGetValue(nodeId, out var node))
            {
                node.LastHeartbeat = DateTime.UtcNow;

                // 2. Redis Hash 업데이트
                try
                {
                    var db = _provider.GetDatabase();
                    if (db != null)
                    {
                        string json = JsonSerializer.Serialize(node);
                        db.HashSet(RedisHashKey, nodeId, json);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RedisClusterRegistry] UpdateHeartbeat Failover triggered for '{nodeId}': {ex.Message}");
                }
            }
        }
    }
}
