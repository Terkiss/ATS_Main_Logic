using System;
using System.Collections.Concurrent;
using System.Linq;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 특정 틱 시점의 전역 게임 상태 스냅샷입니다.
    /// </summary>
    public class WorldState
    {
        /// <summary>
        /// 서버 틱 번호
        /// </summary>
        public long TickNumber { get; set; }

        /// <summary>
        /// 스냅샷 생성 시각
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 엔티티 ID를 키로 하는 엔티티 목록
        /// </summary>
        public ConcurrentDictionary<int, GameEntity> Entities { get; set; } = new();

        /// <summary>
        /// 스냅샷의 전체 깊은 복사를 수행합니다.
        /// </summary>
        public WorldState DeepClone()
        {
            var clone = new WorldState
            {
                TickNumber = this.TickNumber,
                Timestamp = this.Timestamp
            };

            foreach (var kvp in this.Entities)
            {
                clone.Entities.TryAdd(kvp.Key, kvp.Value.DeepClone());
            }

            return clone;
        }
    }
}
