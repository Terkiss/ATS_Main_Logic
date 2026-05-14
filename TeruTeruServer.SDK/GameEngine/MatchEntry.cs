using System;

namespace TeruTeruServer.SDK.GameEngine
{
    public class MatchEntry
    {
        public int HostId { get; set; }
        public int Mmr { get; set; }
        public DateTime EnqueuedUtc { get; set; } = DateTime.UtcNow;
    }
}
