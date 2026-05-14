using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    public class RollingUpdateCoordinator
    {
        private readonly IClusterRegistry _registry;
        private readonly IGameSessionManager _sessionManager;

        public RollingUpdateCoordinator(IClusterRegistry registry, IGameSessionManager sessionManager)
        {
            _registry = registry;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// 현재 노드를 Draining 상태로 전환합니다. (M12)
        /// 새로운 세션을 받지 않고, 기존 세션이 모두 종료되면 Down으로 전환됩니다.
        /// </summary>
        public bool StartDraining(string nodeId)
        {
            var node = _registry.GetNode(nodeId);
            if (node == null) return false;
            
            node.Status = "Draining";
            return true;
        }

        /// <summary>
        /// Draining 상태의 노드가 안전하게 종료 가능한지 확인합니다. (M12)
        /// </summary>
        public bool IsReadyForShutdown(string nodeId)
        {
            var node = _registry.GetNode(nodeId);
            if (node == null || node.Status != "Draining") return false;
            
            return node.ActiveSessionCount == 0;
        }

        /// <summary>
        /// 클러스터 전체에서 업데이트가 가능한 노드를 순서대로 반환합니다. (M12)
        /// 세션이 없는 노드 우선.
        /// </summary>
        public IReadOnlyList<ClusterNodeInfo> GetUpdateOrder()
        {
            return _registry.GetActiveNodes()
                .OrderBy(n => n.ActiveSessionCount)
                .ToList();
        }
    }
}
