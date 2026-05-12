using System;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.Runtime.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using Moq;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class WorldManagementTests
    {
        [Fact]
        public void SpatialGrid_ShouldFilterByInterestArea()
        {
            var grid = new SpatialGrid(cellSize: 50f);
            int zoneId = 1;

            // 엔티티 배치
            grid.UpdateEntityPosition(zoneId, 1, 10, 10);   // Cell (0,0)
            grid.UpdateEntityPosition(zoneId, 2, 40, 40);   // Cell (0,0)
            grid.UpdateEntityPosition(zoneId, 3, 100, 100); // Cell (2,2) - 멀리 떨어짐

            // (10,10) 주변 엔티티 조회 (3x3 셀 범위)
            var nearby = grid.GetNearbyEntityIds(zoneId, 10, 10, 10);

            Assert.Contains(1, nearby);
            Assert.Contains(2, nearby);
            Assert.DoesNotContain(3, nearby); // (2,2)는 (0,0)의 인접 셀이 아님 (0,0, 0,1, 1,0, 1,1 만 인접)
        }

        [Fact]
        public void ZoneManager_JoinAndLeave_ShouldUpdateCorrectState()
        {
            var mockAoI = new Mock<IAoIFilter>();
            var manager = new ZoneManager(mockAoI.Object);
            var zone = manager.CreateZone("TestZone");

            manager.JoinZone(zone.ZoneId, 100);
            Assert.Single(zone.PlayerHostIds);
            Assert.Equal(zone.ZoneId, manager.GetPlayerZone(100)?.ZoneId);
            mockAoI.Verify(a => a.UpdateEntityPosition(zone.ZoneId, 100, It.IsAny<float>(), It.IsAny<float>()), Times.Once);

            manager.LeaveZone(zone.ZoneId, 100);
            Assert.Empty(zone.PlayerHostIds);
            Assert.Null(manager.GetPlayerZone(100));
            mockAoI.Verify(a => a.RemoveEntity(zone.ZoneId, 100), Times.Once);
        }

        [Fact]
        public void ZoneManager_TransferPlayer_ShouldMoveBetweenZones()
        {
            var mockAoI = new Mock<IAoIFilter>();
            var manager = new ZoneManager(mockAoI.Object);
            var zone1 = manager.CreateZone("Zone1");
            var zone2 = manager.CreateZone("Zone2");

            manager.JoinZone(zone1.ZoneId, 100);
            
            var request = new ZoneTransferRequest
            {
                HostId = 100,
                FromZoneId = zone1.ZoneId,
                ToZoneId = zone2.ZoneId,
                SpawnX = 500, SpawnZ = 500
            };

            bool result = manager.TransferPlayer(request);

            Assert.True(result);
            Assert.Empty(zone1.PlayerHostIds);
            Assert.Single(zone2.PlayerHostIds);
            Assert.Equal(zone2.ZoneId, manager.GetPlayerZone(100)?.ZoneId);
            
            var entity = zone2.State.Entities[100];
            Assert.Equal(500, entity.X);
        }

        [Fact]
        public void ZoneFactory_Cleanup_ShouldRemoveOldEmptyInstances()
        {
            var mockAoI = new Mock<IAoIFilter>();
            var manager = new ZoneManager(mockAoI.Object);
            var factory = new ZoneFactory(manager, mockAoI.Object);

            var zone = factory.CreateInstance("Dungeon");
            // 생성 직후엔 아직 1분이 안 지났으므로 정리 안 되어야 함
            int cleaned = factory.CleanupEmptyInstances();
            Assert.Equal(0, cleaned);

            // 강제로 시간 조작 (실제 구현에선 DateTime.UtcNow를 사용하므로 테스트에선 생성 시간 조작 가능하게 설계하거나 Mocking 필요)
            // 여기서는 단순 루프 보다는 Mocking 없이 검증하기 위해 Zone.CreatedUtc를 직접 수정 (SDK 모델이므로 가능)
            zone.CreatedUtc = DateTime.UtcNow.AddMinutes(-2);
            
            cleaned = factory.CleanupEmptyInstances();
            Assert.Equal(1, cleaned);
            Assert.Null(manager.GetZone(zone.ZoneId));
        }
    }
}
