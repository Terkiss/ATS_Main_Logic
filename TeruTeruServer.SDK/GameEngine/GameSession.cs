using System;
using System.Collections.Generic;

namespace TeruTeruServer.SDK.GameEngine
{
    public class GameSession
    {
        public int SessionId { get; set; }
        public GameSessionState State { get; set; } = GameSessionState.Lobby;
        public List<int> PlayerHostIds { get; set; } = new();
        public List<int> SpectatorHostIds { get; set; } = new();
        public Dictionary<int, int> TeamAssignments { get; set; } = new(); // hostId -> teamId
        public RoomState Room { get; set; } = new();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public int WinningTeamId { get; set; } = -1;
    }
}
