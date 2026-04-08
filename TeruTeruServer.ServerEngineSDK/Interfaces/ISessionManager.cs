using System.Collections.Concurrent;
using System.Net.Sockets;

namespace TeruTeruServer.ServerEngineSDK.Interfaces
{
    /// <summary>
    /// 서버의 클라이언트 세션 및 소켓 연결을 관리하는 인터페이스입니다.
    /// </summary>
    public interface ISessionManager
    {
        ConcurrentDictionary<int, Socket> Players { get; }
        bool TryAddPlayer(int hostId, Socket socket);
        bool TryRemovePlayer(int hostId, out Socket socket);
        bool TryGetHostIdBySocket(Socket socket, out int hostId);
    }

    public class SessionManager : ISessionManager
    {
        public ConcurrentDictionary<int, Socket> Players { get; } = new ConcurrentDictionary<int, Socket>();

        public bool TryAddPlayer(int hostId, Socket socket) => Players.TryAdd(hostId, socket);

        public bool TryRemovePlayer(int hostId, out Socket socket) => Players.TryRemove(hostId, out socket);

        public bool TryGetHostIdBySocket(Socket socket, out int hostId)
        {
            foreach (var kvp in Players)
            {
                if (kvp.Value == socket)
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
