using System;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 존(Zone) 생성을 담당하고 유효하지 않은 인스턴스를 정리하는 팩토리 클래스입니다.
    /// </summary>
    public class ZoneFactory
    {
        private readonly IZoneManager _zoneManager;
        private readonly IAoIFilter _aoiFilter;

        public ZoneFactory(IZoneManager zoneManager, IAoIFilter aoiFilter)
        {
            _zoneManager = zoneManager;
            _aoiFilter = aoiFilter;
        }

        /// <summary>
        /// 인스턴스 던전 등 동적 존을 생성합니다.
        /// </summary>
        public Zone CreateInstance(string templateName)
        {
            return _zoneManager.CreateZone(templateName, isInstance: true);
        }

        /// <summary>
        /// 플레이어가 없는 오래된 인스턴스 존을 정리합니다.
        /// </summary>
        public int CleanupEmptyInstances()
        {
            int count = 0;
            var zones = _zoneManager.GetAllZones();
            var now = DateTime.UtcNow;

            foreach (var zone in zones)
            {
                if (zone.IsInstance && zone.PlayerHostIds.Count == 0)
                {
                    // 생성된 지 1분 이상 지난 빈 인스턴스만 삭제
                    if ((now - zone.CreatedUtc) > TimeSpan.FromMinutes(1))
                    {
                        if (_zoneManager.DestroyZone(zone.ZoneId))
                        {
                            count++;
                        }
                    }
                }
            }

            if (count > 0)
            {
                TeruTeruLogger.LogInfo($"Cleaned up {count} empty instance zones.");
            }
            return count;
        }
    }
}
