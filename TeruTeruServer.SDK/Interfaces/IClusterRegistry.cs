using System.Collections.Generic;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 클러스터 내의 노드 등록 및 조회를 관리하는 인터페이스입니다.
    /// </summary>
    public interface IClusterRegistry
    {
        /// <summary>
        /// 새로운 노드를 클러스터에 등록합니다.
        /// </summary>
        void RegisterNode(Clustering.ClusterNodeInfo node);

        /// <summary>
        /// 지정된 노드를 클러스터에서 해제합니다.
        /// </summary>
        void DeregisterNode(string nodeId);

        /// <summary>
        /// 지정된 노드의 정보를 조회합니다.
        /// </summary>
        Clustering.ClusterNodeInfo? GetNode(string nodeId);

        /// <summary>
        /// 현재 활성화된 모든 노드 목록을 반환합니다.
        /// </summary>
        IReadOnlyList<Clustering.ClusterNodeInfo> GetActiveNodes();

        /// <summary>
        /// 노드의 하트비트를 갱신합니다.
        /// </summary>
        void UpdateHeartbeat(string nodeId);
    }
}
