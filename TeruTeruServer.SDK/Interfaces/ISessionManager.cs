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
        public ConcurrentDictionary<int, ClientSession> Players { get; } = new ConcurrentDictionary<int, ClientSession>();

        public bool TryAddPlayer(int hostId, ClientSession session) => Players.TryAdd(hostId, session);

        public bool EvictSession(int hostId, out ClientSession session) => Players.TryRemove(hostId, out session);

        public bool MarkAsGrace(int hostId)
        {
            if (Players.TryGetValue(hostId, out var session))
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
