using System;
using System.Collections.Concurrent;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Logic.Default.P2P
{
    public class RelayQoSController
    {
        private readonly ConcurrentDictionary<int, SessionTraffic> _trafficMap = new ConcurrentDictionary<int, SessionTraffic>();
        private readonly long _bandwidthLimitBytesPerSec;

        public RelayQoSController(long bandwidthLimitBytesPerSec = 1024 * 1024) // Default 1MB/s
        {
            _bandwidthLimitBytesPerSec = bandwidthLimitBytesPerSec;
        }

        public bool CheckAllow(int hostId, int packetSize)
        {
            var traffic = _trafficMap.GetOrAdd(hostId, _ => new SessionTraffic());
            return traffic.CheckAndAdd(packetSize, _bandwidthLimitBytesPerSec);
        }

        private class SessionTraffic
        {
            private long _totalBytesInCurrentSecond;
            private long _lastSecondStartTicks = Environment.TickCount64;
            private readonly object _lock = new object();

            public bool CheckAndAdd(int packetSize, long limit)
            {
                lock (_lock)
                {
                    long now = Environment.TickCount64;
                    if (now - _lastSecondStartTicks >= 1000)
                    {
                        _totalBytesInCurrentSecond = 0;
                        _lastSecondStartTicks = now;
                    }

                    if (_totalBytesInCurrentSecond + packetSize > limit)
                    {
                        return false;
                    }

                    _totalBytesInCurrentSecond += packetSize;
                    return true;
                }
            }
        }
    }
}
