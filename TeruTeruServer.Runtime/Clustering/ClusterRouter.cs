using System.Linq;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.Clustering
{
    public class ClusterRouter
    {
        private readonly IClusterRegistry _registry;

        public ClusterRouter(IClusterRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// 가장 부하가 적은 Active 노드를 선택합니다. (M12)
        /// </summary>
        public ClusterNodeInfo? SelectLeastLoadedNode()
        {
            return _registry.GetActiveNodes()
                .Where(n => n.Status == "Active")
                .OrderBy(n => n.CurrentConnections)
                .FirstOrDefault();
        }

        /// <summary>
        /// 특정 Zone이 있는 노드를 찾습니다. (M12)
        /// TODO: Zone 위치 추적 로직 연동
        /// </summary>
        public ClusterNodeInfo? FindNodeForZone(int zoneId)
        {
            // 현재는 단순 구현: 모든 노드를 순회하며 해당 Zone이 있는지 확인하거나 
            // 중앙 저장소(Redis)에서 ZoneId -> NodeId 매핑을 조회해야 함
            return _registry.GetActiveNodes()
                .FirstOrDefault(n => n.Status == "Active"); // 임시 반환
        }
    }
}
