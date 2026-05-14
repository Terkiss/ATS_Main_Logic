using System.Collections.Generic;
using System.Linq;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// RTT 샘플을 관리하고 이동 평균(Rolling Average)을 계산하는 클래스입니다.
    /// </summary>
    public class RttTracker
    {
        private readonly int _sampleCount;
        private readonly Queue<long> _samples = new();
        private readonly object _lock = new();

        public RttTracker(int sampleCount = 10)
        {
            _sampleCount = sampleCount;
        }

        /// <summary>
        /// 새로운 RTT 샘플을 추가하고 이동 평균을 반환합니다.
        /// </summary>
        public long AddSample(long rttMs)
        {
            lock (_lock)
            {
                _samples.Enqueue(rttMs);
                if (_samples.Count > _sampleCount)
                {
                    _samples.Dequeue();
                }

                AverageRttMs = (long)_samples.Average();
                
                // Jitter 계산 (Max - Min)
                if (_samples.Count > 1)
                {
                    JitterMs = _samples.Max() - _samples.Min();
                }
                
                return AverageRttMs;
            }
        }

        /// <summary>
        /// 현재 이동 평균 RTT (ms)
        /// </summary>
        public long AverageRttMs { get; private set; }

        /// <summary>
        /// RTT 변동폭 (Jitter, ms)
        /// </summary>
        public long JitterMs { get; private set; }
    }
}
