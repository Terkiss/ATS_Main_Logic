using System;
using System.Linq;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Clustering
{
    public class ClusterDashboard
    {
        private readonly IClusterRegistry _registry;
        private readonly ISessionManager _sessionManager;
        private readonly IZoneManager _zoneManager;
        private readonly IGameSessionManager _gameSessionManager;

        public ClusterDashboard(
            IClusterRegistry registry, 
            ISessionManager sessionManager, 
            IZoneManager zoneManager, 
            IGameSessionManager gameSessionManager)
        {
            _registry = registry;
            _sessionManager = sessionManager;
            _zoneManager = zoneManager;
            _gameSessionManager = gameSessionManager;
        }

        public DashboardSnapshot GetSnapshot()
        {
            var nodes = _registry.GetActiveNodes();
            return new DashboardSnapshot
            {
                TotalNodes = nodes.Count,
                ActiveNodes = nodes.Count(n => n.Status == "Active"),
                DrainingNodes = nodes.Count(n => n.Status == "Draining"),
                DownNodes = nodes.Count(n => n.Status == "Down"),
                TotalCcu = nodes.Sum(n => n.CurrentConnections),
                TotalZones = nodes.Sum(n => n.ActiveZoneCount),
                TotalSessions = nodes.Sum(n => n.ActiveSessionCount),
                Tps = ServerMetrics.Tps,
                AverageLatencyMs = ServerMetrics.AverageLatencyMs,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public class DashboardSnapshot
    {
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public int DrainingNodes { get; set; }
        public int DownNodes { get; set; }
        public int TotalCcu { get; set; }
        public int TotalZones { get; set; }
        public int TotalSessions { get; set; }
        public long Tps { get; set; }
        public long AverageLatencyMs { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
