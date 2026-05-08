using System.Collections.Concurrent;
using System.Collections.Generic;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 룸 단위 브로드캐스트를 처리하는 구현체입니다.
    /// </summary>
    public class RoomBroadcaster : IRoomBroadcaster
    {
        private readonly IMessageSender _messageSender;
        private readonly ISessionManager _sessionManager;
        private readonly ConcurrentDictionary<int, RoomState> _rooms = new();

        public RoomBroadcaster(IMessageSender messageSender, ISessionManager sessionManager)
        {
            _messageSender = messageSender;
            _sessionManager = sessionManager;
        }

        public void RegisterRoom(RoomState room)
        {
            _rooms[room.RoomId] = room;
        }

        public void UnregisterRoom(int roomId)
        {
            _rooms.TryRemove(roomId, out _);
        }

        public void BroadcastToRoom(int roomId, byte[] packet)
        {
            BroadcastToRoom(roomId, packet, -1);
        }

        public void BroadcastToRoom(int roomId, byte[] packet, int excludeHostId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;

            // ParticipantHostIds를 순회하며 전송
            // 락 없이 순회하기 위해 스냅샷 리스트를 사용하거나 Concurrent 구조 권장
            // 여기서는 List<int>이므로 룸 데이터 변경 시의 안정성을 고려하여 복사본 사용 고려
            List<int> targets;
            lock (room.ParticipantHostIds)
            {
                targets = new List<int>(room.ParticipantHostIds);
            }

            foreach (var hostId in targets)
            {
                if (hostId == excludeHostId) continue;
                
                // IMessageSender를 통해 실제 소켓 전송
                _messageSender.SendData(hostId, packet);
            }
        }
    }
}
