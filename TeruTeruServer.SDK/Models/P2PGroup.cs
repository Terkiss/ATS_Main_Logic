using System.Collections.Concurrent;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.SDK.Models
{
    public class P2PGroup
    {
        public int GroupId { get; private set; }
        public int OwnerId { get; private set; }

        // ConcurrentBag to handle multi-threaded Add/Remove safely
        private ConcurrentDictionary<int, byte> _members = new ConcurrentDictionary<int, byte>();

        public P2PGroup(int ownerId)
        {
            GroupId = ServerMemory.GetHostID; // 유저와 동일한 전역 채번기 사용
            OwnerId = ownerId;
            AddMember(ownerId);
        }

        public void AddMember(int hostId)
        {
            _members.TryAdd(hostId, 0);
        }

        public void RemoveMember(int hostId)
        {
            _members.TryRemove(hostId, out _);
        }

        public int[] GetMemberIds()
        {
            return _members.Keys.ToArray();
        }
    }
}
