using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 게임 월드 내의 존(Zone)들과 플레이어 입퇴장을 관리하는 클래스입니다.
    /// </summary>
    public class ZoneManager : IZoneManager
    {
        private readonly GameWorld _world = new();
        private readonly ConcurrentDictionary<int, int> _playerZoneMap = new(); // hostId -> zoneId
        private readonly IAoIFilter _aoiFilter;
        private int _nextZoneId = 1;
        private readonly object _idLock = new();

        public ZoneManager(IAoIFilter aoiFilter)
        {
            _aoiFilter = aoiFilter;
        }

        public Zone CreateZone(string name, bool isInstance = false)
        {
            int id;
            lock (_idLock) { id = _nextZoneId++; }

            var zone = new Zone
            {
                ZoneId = id,
                ZoneName = name,
                IsInstance = isInstance
            };

            _world.Zones[id] = zone;
            TeruTeruLogger.LogInfo($"Zone Created: {name} (ID: {id}, Instance: {isInstance})");
            return zone;
        }

        public bool DestroyZone(int zoneId)
        {
            if (_world.Zones.TryRemove(zoneId, out var zone))
            {
                // 존에 남은 플레이어 강제 퇴장 처리 등 필요 시 추가
                TeruTeruLogger.LogInfo($"Zone Destroyed: {zone.ZoneName} (ID: {zoneId})");
                return true;
            }
            return false;
        }

        public Zone? GetZone(int zoneId)
        {
            _world.Zones.TryGetValue(zoneId, out var zone);
            return zone;
        }

        public IReadOnlyList<Zone> GetAllZones()
        {
            return _world.Zones.Values.ToList();
        }

        public bool JoinZone(int zoneId, int hostId)
        {
            var zone = GetZone(zoneId);
            if (zone == null) return false;

            lock (zone.PlayerHostIds)
            {
                if (!zone.PlayerHostIds.Contains(hostId))
                {
                    zone.PlayerHostIds.Add(hostId);
                }
            }

            // 기본 플레이어 엔티티 생성 및 등록
            var entity = new GameEntity
            {
                EntityId = hostId, // 플레이어는 HostID를 EntityId로 사용 (단순화)
                OwnerHostId = hostId,
                IsDirty = true
            };
            zone.State.Entities[hostId] = entity;
            _playerZoneMap[hostId] = zoneId;

            // AoI 초기 위치 등록
            _aoiFilter.UpdateEntityPosition(zoneId, hostId, entity.X, entity.Z);

            TeruTeruLogger.LogInfo($"Player {hostId} joined Zone {zoneId}");
            return true;
        }

        public bool LeaveZone(int zoneId, int hostId)
        {
            var zone = GetZone(zoneId);
            if (zone == null) return false;

            lock (zone.PlayerHostIds)
            {
                zone.PlayerHostIds.Remove(hostId);
            }

            zone.State.Entities.TryRemove(hostId, out _);
            _playerZoneMap.TryRemove(hostId, out _);
            _aoiFilter.RemoveEntity(zoneId, hostId);

            TeruTeruLogger.LogInfo($"Player {hostId} left Zone {zoneId}");
            return true;
        }

        public Zone? GetPlayerZone(int hostId)
        {
            if (_playerZoneMap.TryGetValue(hostId, out int zoneId))
            {
                return GetZone(zoneId);
            }
            return null;
        }

        public bool TransferPlayer(ZoneTransferRequest request)
        {
            var fromZone = GetZone(request.FromZoneId);
            var toZone = GetZone(request.ToZoneId);

            if (toZone == null) return false;

            // 1. 기존 존에서 퇴장
            if (fromZone != null)
            {
                LeaveZone(request.FromZoneId, request.HostId);
            }

            // 2. 새 존에 입장
            JoinZone(request.ToZoneId, request.HostId);

            // 3. 위치 설정 및 AoI 갱신
            var entity = toZone.State.Entities[request.HostId];
            entity.X = request.SpawnX;
            entity.Y = request.SpawnY;
            entity.Z = request.SpawnZ;
            _aoiFilter.UpdateEntityPosition(request.ToZoneId, request.HostId, entity.X, entity.Z);

            TeruTeruLogger.LogInfo($"Player {request.HostId} transferred from {request.FromZoneId} to {request.ToZoneId}");
            return true;
        }
    }
}
