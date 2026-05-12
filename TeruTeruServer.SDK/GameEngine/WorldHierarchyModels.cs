using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 게임 내 공간 단위인 존(Zone) 모델입니다.
    /// 필드, 마을, 던전 인스턴스 등을 나타냅니다.
    /// </summary>
    public class Zone
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = "";
        
        /// <summary>
        /// 존 내부의 게임 상태 (해당 존에 속한 엔티티만 포함)
        /// </summary>
        public WorldState State { get; set; } = new();

        /// <summary>
        /// 존에 현재 머물고 있는 플레이어들의 HostID 목록
        /// </summary>
        public List<int> PlayerHostIds { get; set; } = new();

        /// <summary>
        /// 인스턴스 던전 여부
        /// </summary>
        public bool IsInstance { get; set; }

        /// <summary>
        /// 존 생성 시각 (인스턴스 정리 시 사용)
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 여러 존을 포함하는 전역 게임 월드 모델입니다.
    /// </summary>
    public class GameWorld
    {
        public ConcurrentDictionary<int, Zone> Zones { get; set; } = new();
        public string WorldName { get; set; } = "Default";
    }
}
