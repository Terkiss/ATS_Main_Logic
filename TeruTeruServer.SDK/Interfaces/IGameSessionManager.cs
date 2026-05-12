using System.Collections.Generic;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.SDK.Interfaces
{
    public interface IGameSessionManager
    {
        GameSession CreateSession(List<MatchEntry> players, int teamCount);
        bool TransitionState(int sessionId, GameSessionState newState);
        GameSession? GetSession(int sessionId);
        GameSession? GetPlayerSession(int hostId);
        bool AddSpectator(int sessionId, int hostId);
        bool RemovePlayer(int sessionId, int hostId);
        bool RejoinPlayer(int sessionId, int hostId);
        IReadOnlyList<GameSession> GetActiveSessions();
    }
}
