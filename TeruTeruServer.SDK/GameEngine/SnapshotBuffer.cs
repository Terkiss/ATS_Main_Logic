using System.Collections.Concurrent;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 과거의 WorldState 스냅샷들을 보관하는 링 버퍼입니다.
    /// </summary>
    public class SnapshotBuffer
    {
        private readonly WorldState?[] _buffer;
        private readonly int _capacity;
        private long _latestTick = -1;

        public SnapshotBuffer(int capacity = 128)
        {
            _capacity = capacity;
            _buffer = new WorldState[capacity];
        }

        /// <summary>
        /// 새로운 스냅샷을 버퍼에 추가합니다.
        /// </summary>
        public void Push(WorldState state)
        {
            int index = (int)(state.TickNumber % _capacity);
            _buffer[index] = state;
            
            if (state.TickNumber > _latestTick)
            {
                _latestTick = state.TickNumber;
            }
        }

        /// <summary>
        /// 특정 틱 번호의 스냅샷을 조회합니다.
        /// </summary>
        public WorldState? GetAtTick(long tickNumber)
        {
            int index = (int)(tickNumber % _capacity);
            var state = _buffer[index];
            
            if (state != null && state.TickNumber == tickNumber)
            {
                return state;
            }
            
            return null;
        }

        /// <summary>
        /// 가장 최신 스냅샷을 조회합니다.
        /// </summary>
        public WorldState? GetLatest()
        {
            if (_latestTick == -1) return null;
            return GetAtTick(_latestTick);
        }
    }
}
