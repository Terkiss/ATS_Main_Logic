using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.SDK.Clustering
{
    /// <summary>
    /// 로컬 메모리 기반의 세션 저장소 구현체입니다.
    /// </summary>
    public class InMemorySessionStore : ISessionStore
    {
        // SessionManager.Players와의 호환성을 위해 직접 노출 가능한 ConcurrentDictionary 사용
        public ConcurrentDictionary<int, ClientSession> InternalDictionary { get; } = new ConcurrentDictionary<int, ClientSession>();

        public bool TryAdd(int hostId, ClientSession session) => InternalDictionary.TryAdd(hostId, session);

        public bool TryGet(int hostId, out ClientSession session) => InternalDictionary.TryGetValue(hostId, out session);

        public bool TryRemove(int hostId, out ClientSession session) => InternalDictionary.TryRemove(hostId, out session);

        public ClientSession? FindByReconnectToken(string token)
        {
            return InternalDictionary.Values.FirstOrDefault(s => s.ReconnectToken == token);
        }

        public IEnumerable<ClientSession> GetAll() => InternalDictionary.Values;
    }
}
