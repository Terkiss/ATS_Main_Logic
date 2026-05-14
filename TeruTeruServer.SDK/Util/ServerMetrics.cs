using System.Threading;

namespace TeruTeruServer.SDK.Util
{
    public static class ServerMetrics
    {
        private static long _processedPacketCount = 0;
        private static long _lastProcessedPacketCount = 0;
        public static long Tps { get; private set; }

        public static void IncrementPacketCount()
        {
            Interlocked.Increment(ref _processedPacketCount);
        }

        public static long GetProcessedPacketCount()
        {
            return Interlocked.Read(ref _processedPacketCount);
        }

        public static void UpdateTps()
        {
            long currentCount = Interlocked.Read(ref _processedPacketCount);
            Tps = currentCount - _lastProcessedPacketCount;
            _lastProcessedPacketCount = currentCount;
        }

        // 대시보드 메트릭 (Milestone 12)
        private static int _concurrentConnections = 0;
        private static int _activeZoneCount = 0;
        private static int _activeSessionCount = 0;
        private static long _averageLatencyMs = 0;

        public static int ConcurrentConnections => _concurrentConnections;
        public static int ActiveZoneCount => _activeZoneCount;
        public static int ActiveSessionCount => _activeSessionCount;
        public static long AverageLatencyMs => _averageLatencyMs;

        public static void UpdateCcu(int count) => Interlocked.Exchange(ref _concurrentConnections, count);
        public static void UpdateZoneCount(int count) => Interlocked.Exchange(ref _activeZoneCount, count);
        public static void UpdateSessionCount(int count) => Interlocked.Exchange(ref _activeSessionCount, count);
        public static void UpdateLatency(long ms) => Interlocked.Exchange(ref _averageLatencyMs, ms);
    }
}
