using System.Collections.Generic;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 두 월드 상태 간의 차이(Delta)를 계산하는 유틸리티입니다.
    /// </summary>
    public static class DeltaCalculator
    {
        /// <summary>
        /// 현재 상태에서 변경된(IsDirty) 엔티티들만 추출하여 반환하고, 플래그를 리셋합니다.
        /// </summary>
        public static List<GameEntity> CalculateDelta(WorldState previous, WorldState current)
        {
            var deltaList = new List<GameEntity>();

            foreach (var kvp in current.Entities)
            {
                var entity = kvp.Value;
                
                // 1. 명시적으로 IsDirty 플래그가 설정된 경우
                // 2. 이전 상태에 존재하지 않았던 경우 (신규 엔티티)
                if (entity.IsDirty || !previous.Entities.ContainsKey(entity.EntityId))
                {
                    deltaList.Add(entity.DeepClone());
                    entity.IsDirty = false; // 전송 대기열에 포함되었으므로 리셋
                }
            }

            return deltaList;
        }
    }
}
