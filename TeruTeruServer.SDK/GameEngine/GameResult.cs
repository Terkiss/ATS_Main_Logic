using System;
using System.Collections.Generic;

namespace TeruTeruServer.SDK.GameEngine
{
    public class GameResult
    {
        public int SessionId { get; set; }
        public int WinningTeamId { get; set; }
        public Dictionary<int, int> PlayerTeams { get; set; } = new(); // hostId -> teamId
        public TimeSpan Duration { get; set; }
        public DateTime EndedUtc { get; set; } = DateTime.UtcNow;
    }
}
