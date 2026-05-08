using System.Collections.Generic;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 게임 룸(인스턴스)의 상태를 관리하는 클래스입니다.
    /// </summary>
    public class RoomState
    {
        /// <summary>
        /// 룸 고유 ID
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// 룸에 참여 중인 클라이언트 HostID 목록
        /// </summary>
        public List<int> ParticipantHostIds { get; set; } = new();

        /// <summary>
        /// 룸 내의 현재 게임 상태
        /// </summary>
        public WorldState CurrentState { get; set; } = new();
    }
}
