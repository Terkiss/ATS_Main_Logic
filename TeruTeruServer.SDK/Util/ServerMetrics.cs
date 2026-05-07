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
    }
}
