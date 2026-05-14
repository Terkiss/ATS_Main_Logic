using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.SDK.Clustering
{
    /// <summary>
    /// 로컬 단일 노드를 위한 클러스터 레지스트리 구현체입니다.
    /// </summary>
    public class LocalClusterRegistry : IClusterRegistry
    {
        private readonly ConcurrentDictionary<string, ClusterNodeInfo> _nodes = new ConcurrentDictionary<string, ClusterNodeInfo>();

        public void RegisterNode(ClusterNodeInfo node)
        {
            _nodes[node.NodeId] = node;
        }

        public void DeregisterNode(string nodeId)
        {
            _nodes.TryRemove(nodeId, out _);
        }

        public ClusterNodeInfo? GetNode(string nodeId)
        {
            _nodes.TryGetValue(nodeId, out var node);
            return node;
        }

        public IReadOnlyList<ClusterNodeInfo> GetActiveNodes()
        {
            return _nodes.Values.Where(n => n.Status == "Active").ToList().AsReadOnly();
        }

        public void UpdateHeartbeat(string nodeId)
        {
            if (_nodes.TryGetValue(nodeId, out var node))
            {
                node.LastHeartbeat = DateTime.UtcNow;
            }
        }
    }
}
