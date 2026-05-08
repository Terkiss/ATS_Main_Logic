using System.Collections.Concurrent;
using System.Net.Sockets;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 서버의 클라이언트 세션 연결을 관리하는 인터페이스입니다.
    /// </summary>
    public interface ISessionManager
    {
        ConcurrentDictionary<int, ClientSession> Players { get; }
        bool TryAddPlayer(int hostId, ClientSession session);
        bool EvictSession(int hostId, out ClientSession session);
        bool MarkAsGrace(int hostId);
        bool TryGetHostIdBySocket(Socket socket, out int hostId);
    }

    public class SessionManager : ISessionManager
    {
        private readonly ISessionStore _store;

        // 기존 코드 호환성을 위해 ConcurrentDictionary 반환 유지
        public ConcurrentDictionary<int, ClientSession> Players { get; }

        public SessionManager(ISessionStore store)
        {
            _store = store;
            if (store is Clustering.InMemorySessionStore inMemory)
            {
                Players = inMemory.InternalDictionary;
            }
            else
            {
                // 분산 저장소(Redis 등) 사용 시 로컬 캐시용으로만 활용되거나, 
                // 전체 목록 조회가 빈번할 경우 별도 동기화 로직이 필요할 수 있음.
                // 현재는 InMemorySessionStore 기반으로 동작함을 보장.
                Players = new ConcurrentDictionary<int, ClientSession>();
            }
        }

        public bool TryAddPlayer(int hostId, ClientSession session) => _store.TryAdd(hostId, session);

        public bool EvictSession(int hostId, out ClientSession session) => _store.TryRemove(hostId, out session);

        public bool MarkAsGrace(int hostId)
        {
            if (_store.TryGet(hostId, out var session))
            {
                session.State = SessionState.Grace;
                session.ClientSocket = null;
                return true;
            }
            return false;
        }

        public bool TryGetHostIdBySocket(Socket socket, out int hostId)
        {
            if (socket == null)
            {
                hostId = -1;
                return false;
            }

            foreach (var session in _store.GetAll())
            {
                if (session.ClientSocket == socket)
                {
                    // ClientSession 내부에 HostID가 보관되어 있다고 가정하거나 
                    // ISessionStore에 역조회 기능을 추가하는 것이 더 효율적이나, 
                    // 현재는 기존 로직을 유지하기 위해 순회 방식을 사용.
                    // Note: ClientSession 클래스에 HostID 속성이 있는 경우를 대비해 
                    // 안전한 구현을 위해 내부 딕셔너리 직접 순회(Players 사용) 선호
                    hostId = -1; 
                }
            }

            // 호환성을 위해 Players 프로퍼티(ConcurrentDictionary) 직접 순회 유지
            foreach (var kvp in Players)
            {
                if (kvp.Value.ClientSocket == socket)
                {
                    hostId = kvp.Key;
                    return true;
                }
            }

            hostId = -1;
            return false;
        }
    }
}
