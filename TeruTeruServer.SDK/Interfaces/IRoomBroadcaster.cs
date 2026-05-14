using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 특정 룸 내의 플레이어들에게 효율적으로 패킷을 브로드캐스팅하는 인터페이스입니다.
    /// </summary>
    public interface IRoomBroadcaster
    {
        /// <summary>
        /// 룸 내 모든 인원에게 전송합니다.
        /// </summary>
        void BroadcastToRoom(int roomId, byte[] packet);

        /// <summary>
        /// 특정 인원을 제외한 룸 내 모든 인원에게 전송합니다.
        /// </summary>
        void BroadcastToRoom(int roomId, byte[] packet, int excludeHostId);

        /// <summary>
        /// 브로드캐스트 대상 룸 상태를 등록합니다.
        /// </summary>
        void RegisterRoom(RoomState room);

        /// <summary>
        /// 룸 등록을 해제합니다.
        /// </summary>
        void UnregisterRoom(int roomId);
    }
}
