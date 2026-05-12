using System.Collections.Generic;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 관심 영역(AoI) 기반의 엔티티 필터링을 제공하는 인터페이스입니다.
    /// </summary>
    public interface IAoIFilter
    {
        /// <summary>
        /// 특정 좌표 주변의 엔티티 ID 목록을 조회합니다.
        /// </summary>
        List<int> GetNearbyEntityIds(int zoneId, float x, float z, float radius);

        /// <summary>
        /// 엔티티의 위치 정보를 갱신합니다. (그리드 셀 이동 처리)
        /// </summary>
        void UpdateEntityPosition(int zoneId, int entityId, float x, float z);

        /// <summary>
        /// 존에서 엔티티 정보를 제거합니다.
        /// </summary>
        void RemoveEntity(int zoneId, int entityId);
    }
}
