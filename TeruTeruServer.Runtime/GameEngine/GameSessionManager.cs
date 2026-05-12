using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    public class GameSessionManager : IGameSessionManager
    {
        private readonly ConcurrentDictionary<int, GameSession> _sessions = new();
        private readonly ConcurrentDictionary<int, int> _playerSessionMap = new(); // hostId -> sessionId
        private readonly IEventBus _eventBus;
        private readonly IRoomBroadcaster _roomBroadcaster;
        private readonly TeamBalancer _teamBalancer = new();
        private int _nextSessionId = 1;

        public GameSessionManager(IEventBus eventBus, IRoomBroadcaster roomBroadcaster)
        {
            _eventBus = eventBus;
            _roomBroadcaster = roomBroadcaster;
        }

        public GameSession CreateSession(List<MatchEntry> players, int teamCount)
        {
            int sessionId = System.Threading.Interlocked.Increment(ref _nextSessionId);
            var session = new GameSession
            {
                SessionId = sessionId,
                State = GameSessionState.MatchFound,
                PlayerHostIds = players.Select(p => p.HostId).ToList(),
                TeamAssignments = _teamBalancer.AssignTeams(players, teamCount),
                CreatedUtc = DateTime.UtcNow
            };

            // RoomState 초기화
            session.Room.RoomId = sessionId;
            session.Room.ParticipantHostIds = session.PlayerHostIds.ToList();
            
            _sessions[sessionId] = session;
            foreach (var hostId in session.PlayerHostIds)
            {
                _playerSessionMap[hostId] = sessionId;
            }

            _roomBroadcaster.RegisterRoom(session.Room);
            
            TeruTeruLogger.LogInfo($"[Session] Created Session {sessionId} with {session.PlayerHostIds.Count} players.");
            return session;
        }

        public bool TransitionState(int sessionId, GameSessionState newState)
        {
            if (!_sessions.TryGetValue(sessionId, out var session)) return false;

            lock (session)
            {
                // 상태 전이는 단방향만 허용 (newState > currentState)
                if (newState <= session.State)
                {
                    TeruTeruLogger.LogWarning($"[Session] Invalid state transition in Session {sessionId}: {session.State} -> {newState}");
                    return false;
                }

                var oldState = session.State;
                session.State = newState;
                TeruTeruLogger.LogInfo($"[Session] Session {sessionId} state changed: {oldState} -> {newState}");

                // InGame -> Result 전이 시 OnGameEnd 이벤트 발행
                if (oldState == GameSessionState.InGame && newState == GameSessionState.Result)
                {
                    var result = new GameResult
                    {
                        SessionId = sessionId,
                        WinningTeamId = session.WinningTeamId,
                        PlayerTeams = session.TeamAssignments.ToDictionary(k => k.Key, v => v.Value),
                        Duration = DateTime.UtcNow - session.CreatedUtc,
                        EndedUtc = DateTime.UtcNow
                    };
                    _eventBus.Publish("game:end", result);
                }
                // Result -> Disbanded 전이 시 자원 정리
                else if (newState == GameSessionState.Disbanded)
                {
                    foreach (var hostId in session.PlayerHostIds)
                    {
                        _playerSessionMap.TryRemove(hostId, out _);
                    }
                    foreach (var hostId in session.SpectatorHostIds)
                    {
                        _playerSessionMap.TryRemove(hostId, out _);
                    }
                    _roomBroadcaster.UnregisterRoom(sessionId);
                    _sessions.TryRemove(sessionId, out _);
                    TeruTeruLogger.LogInfo($"[Session] Session {sessionId} disbanded and cleaned up.");
                }
            }

            return true;
        }

        public GameSession? GetSession(int sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        public GameSession? GetPlayerSession(int hostId)
        {
            if (_playerSessionMap.TryGetValue(hostId, out int sessionId))
            {
                return GetSession(sessionId);
            }
            return null;
        }

        public bool AddSpectator(int sessionId, int hostId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session)) return false;
            
            lock (session)
            {
                // InGame 상태인 세션에만 관전자 추가 허용
                if (session.State != GameSessionState.InGame)
                {
                    TeruTeruLogger.LogWarning($"[Session] Cannot add spectator to session {sessionId} in state {session.State}");
                    return false;
                }

                if (!session.SpectatorHostIds.Contains(hostId))
                {
                    session.SpectatorHostIds.Add(hostId);
                    _playerSessionMap[hostId] = sessionId;
                    TeruTeruLogger.LogInfo($"[Session] Spectator {hostId} added to session {sessionId}");
                    return true;
                }
            }
            return false;
        }

        public bool RemovePlayer(int sessionId, int hostId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session)) return false;

            lock (session)
            {
                if (session.PlayerHostIds.Remove(hostId))
                {
                    session.Room.ParticipantHostIds.Remove(hostId);
                    _playerSessionMap.TryRemove(hostId, out _);
                    TeruTeruLogger.LogInfo($"[Session] Player {hostId} removed from session {sessionId}");
                    return true;
                }
                
                if (session.SpectatorHostIds.Remove(hostId))
                {
                    _playerSessionMap.TryRemove(hostId, out _);
                    TeruTeruLogger.LogInfo($"[Session] Spectator {hostId} removed from session {sessionId}");
                    return true;
                }
            }

            return false;
        }

        public bool RejoinPlayer(int sessionId, int hostId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session)) return false;

            lock (session)
            {
                // InGame 상태가 아닌 세션에는 복귀 불가
                if (session.State != GameSessionState.InGame) return false;

                if (session.TeamAssignments.ContainsKey(hostId))
                {
                    if (!session.PlayerHostIds.Contains(hostId))
                    {
                        session.PlayerHostIds.Add(hostId);
                        session.Room.ParticipantHostIds.Add(hostId);
                    }
                    _playerSessionMap[hostId] = sessionId;
                    TeruTeruLogger.LogInfo($"[Session] Player {hostId} rejoined session {sessionId}");
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<GameSession> GetActiveSessions()
        {
            return _sessions.Values.ToList();
        }
    }
}
