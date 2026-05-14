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

        /// <summary>
        /// 현재 동접 수입니다. (M12)
        /// </summary>
        public int CurrentConnections { get; set; }

        /// <summary>
        /// 활성 존 수입니다. (M12)
        /// </summary>
        public int ActiveZoneCount { get; set; }

        /// <summary>
        /// 활성 게임 세션 수입니다. (M12)
        /// </summary>
        public int ActiveSessionCount { get; set; }

        /// <summary>
        /// CPU 사용률 (0-100)입니다. (M12)
        /// </summary>
        public double CpuUsagePercent { get; set; }
    }
}
