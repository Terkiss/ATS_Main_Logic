using System.Collections.Generic;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 클라이언트 세션의 영속성 및 분산 저장을 관리하는 인터페이스입니다.
    /// </summary>
    public interface ISessionStore
    {
        /// <summary>
        /// 새로운 세션을 저장소에 추가합니다.
        /// </summary>
        bool TryAdd(int hostId, ClientSession session);

        /// <summary>
        /// 지정된 HostID의 세션을 조회합니다.
        /// </summary>
        bool TryGet(int hostId, out ClientSession session);

        /// <summary>
        /// 지정된 HostID의 세션을 삭제합니다.
        /// </summary>
        bool TryRemove(int hostId, out ClientSession session);

        /// <summary>
        /// ReconnectToken을 기반으로 세션을 검색합니다. (분산 재연결용)
        /// </summary>
        ClientSession? FindByReconnectToken(string token);

        /// <summary>
        /// 현재 모든 세션 목록을 반환합니다.
        /// </summary>
        IEnumerable<ClientSession> GetAll();
    }
}
