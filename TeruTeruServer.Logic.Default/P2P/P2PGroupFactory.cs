using System.Reflection;
using TeruTeruServer.SDK.Models;

namespace TeruTeruServer.Logic.Default.P2P
{
    public static class P2PGroupFactory
    {
        /// <summary>
        /// 지정된 방장 Host ID와 선택적 Group ID를 사용하여 P2PGroup을 생성합니다.
        /// </summary>
        public static P2PGroup Create(int ownerHostId, int? groupId = null)
        {
            var group = new P2PGroup(ownerHostId);
            if (groupId.HasValue)
            {
                var prop = typeof(P2PGroup).GetProperty("GroupId", BindingFlags.Public | BindingFlags.Instance);
                prop?.SetValue(group, groupId.Value);
            }
            return group;
        }
    }
}
