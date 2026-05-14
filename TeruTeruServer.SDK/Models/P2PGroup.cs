using System.Collections.Concurrent;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Models
{
    public class P2PGroup
    {
        public int GroupId { get; private set; }
        public int OwnerId { get; private set; }

        // ConcurrentBag to handle multi-threaded Add/Remove safely
        private ConcurrentDictionary<int, byte> _members = new ConcurrentDictionary<int, byte>();

        public event Action<int>? OnMemberJoined;
        public event Action<int>? OnMemberLeft;
        public event Action<int, P2PStatus>? OnMemberStatusChanged;

        public P2PGroup(int ownerId)
        {
            GroupId = ServerMemory.GetHostID; // 유저와 동일한 전역 채번기 사용
            OwnerId = ownerId;
            AddMember(ownerId);
        }

        public void AddMember(int hostId)
        {
            if (_members.TryAdd(hostId, 0))
            {
                OnMemberJoined?.Invoke(hostId);
            }
        }

        public void RemoveMember(int hostId)
        {
            if (_members.TryRemove(hostId, out _))
            {
                OnMemberLeft?.Invoke(hostId);
            }
        }

        public void UpdateMemberStatus(int hostId, P2PStatus status)
        {
            if (_members.ContainsKey(hostId))
            {
                OnMemberStatusChanged?.Invoke(hostId, status);
            }
        }

        public int[] GetMemberIds()
        {
            return _members.Keys.ToArray();
        }
    }
}
