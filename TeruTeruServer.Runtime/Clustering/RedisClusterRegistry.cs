using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// Redis 기반 클러스터 레지스트리 시뮬레이션 구현체 (M12)
    /// </summary>
    public class RedisClusterRegistry : IClusterRegistry
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, ClusterNodeInfo> _nodes = new();

        public RedisClusterRegistry(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void RegisterNode(ClusterNodeInfo node)
        {
            // TODO: Redis HSET nodes {node.NodeId} {json}
            _nodes[node.NodeId] = node;
        }

        public void DeregisterNode(string nodeId)
        {
            // TODO: Redis HDEL nodes {nodeId}
            _nodes.TryRemove(nodeId, out _);
        }

        public ClusterNodeInfo? GetNode(string nodeId)
        {
            // TODO: Redis HGET nodes {nodeId}
            return _nodes.TryGetValue(nodeId, out var node) ? node : null;
        }

        public IReadOnlyList<ClusterNodeInfo> GetActiveNodes()
        {
            // TODO: Redis HGETALL nodes
            return _nodes.Values.ToList();
        }

        public void UpdateHeartbeat(string nodeId)
        {
            if (_nodes.TryGetValue(nodeId, out var node))
            {
                node.LastHeartbeat = DateTime.UtcNow;
                // TODO: Redis HSET nodes {nodeId} {json} or EXPIRE
            }
        }
    }
}
