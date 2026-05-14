using System;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Clustering
{
    public class NodeHealthMonitor
    {
        private readonly IClusterRegistry _registry;
        private readonly IEventBus _eventBus;
        private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(30);

        public NodeHealthMonitor(IClusterRegistry registry, IEventBus eventBus)
        {
            _registry = registry;
            _eventBus = eventBus;
        }

        /// <summary>
        /// Tick 핸들러에서 호출. 타임아웃된 노드를 "Down"으로 마킹. (M12)
        /// </summary>
        public void CheckHealth()
        {
            foreach (var node in _registry.GetActiveNodes())
            {
                if (node.Status != "Down" && DateTime.UtcNow - node.LastHeartbeat > _heartbeatTimeout)
                {
                    node.Status = "Down";
                    TeruTeruLogger.LogWarning($"[Cluster] Node {node.NodeId} is marked as DOWN due to heartbeat timeout.");
                    _eventBus.Publish("cluster:node:down", node.NodeId);
                }
            }
        }
    }
}
