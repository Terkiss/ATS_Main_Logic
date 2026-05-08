using System;

namespace TeruTeruServer.SDK.Clustering
{
    /// <summary>
    /// 클러스터에 참여 중인 노드의 정보를 담는 모델입니다.
    /// </summary>
    public class ClusterNodeInfo
    {
        /// <summary>
        /// 노드의 고유 식별자입니다.
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// 노드의 네트워크 주소입니다.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 노드의 서비스 포트입니다.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 노드의 상태입니다. ("Active", "Draining", "Down")
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// 마지막 하트비트 시각입니다.
        /// </summary>
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    }
}
