using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.Runtime.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using Moq;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class LagCompensationTests
    {
        [Fact]
        public void RttTracker_ShouldCalculateCorrectAverage()
        {
            var tracker = new RttTracker(5);
            tracker.AddSample(100);
            tracker.AddSample(200);
            tracker.AddSample(150);

            Assert.Equal(150, tracker.AverageRttMs);
            Assert.Equal(100, tracker.JitterMs); // 200 - 100
        }

        [Fact]
        public void AuthorityValidator_ShouldClampExcessiveSpeed()
        {
            var mockLoop = new Mock<IGameLoop>();
            mockLoop.Setup(l => l.TickRate).Returns(20);
            var mockSecurityLogger = new Mock<ISecurityEventLogger>();
            var mockSessionManager = new Mock<ISessionManager>();
            var sanctionManager = new SanctionManager(mockSecurityLogger.Object, mockSessionManager.Object);
            var validator = new ServerAuthorityValidator(mockLoop.Object, mockSecurityLogger.Object, sanctionManager);
            var entity = new GameEntity { EntityId = 1, X = 0, Z = 0 };
            var input = new GameInput { MoveX = 10.0f, MoveZ = 10.0f }; // 과도한 속도
            
            // maxSpeed = 1.0f (1틱당 최대 1.0f * 0.05s = 0.05f 이동 가능)
            bool result = validator.ValidateAndApply(input, entity, 1.0f);

            Assert.True(result);
            Assert.InRange(entity.X, 0.04f, 0.06f); // 클램프된 속도 1.0f * 0.05s 적용 확인
        }

        [Fact]
        public void LagCompensator_ShouldCalculateCorrectRewindTicks()
        {
            var mockLoop = new Mock<IGameLoop>();
            mockLoop.Setup(l => l.TickRate).Returns(20); // 1 tick = 50ms
            
            var compensator = new LagCompensator(new SnapshotBuffer(), mockLoop.Object);

            // RTT 200ms -> 편도 100ms -> 2 ticks
            Assert.Equal(2, compensator.CalculateRewindTicks(200));
            // RTT 100ms -> 편도 50ms -> 1 tick
            Assert.Equal(1, compensator.CalculateRewindTicks(100));
        }

        [Fact]
        public void LagCompensator_ShouldDetectHitInPast()
        {
            var buffer = new SnapshotBuffer(100);
            var mockLoop = new Mock<IGameLoop>();
            mockLoop.Setup(l => l.TickRate).Returns(20);
            mockLoop.Setup(l => l.CurrentTick).Returns(10); // 현재는 10틱

            // 8틱 시점의 위치 설정 (X=10, Y=0, Z=10)
            var state = new WorldState { TickNumber = 8 };
            state.Entities.TryAdd(1, new GameEntity { EntityId = 1, X = 10, Y = 0, Z = 10, HitboxRadius = 1.0f });
            buffer.Push(state);

            var compensator = new LagCompensator(buffer, mockLoop.Object);

            // 슈터가 100ms 지연(RTT 200ms)이 있을 때, 10틱에서 쏘면 8틱을 봐야 함
            var request = new HitValidationRequest
            {
                TargetEntityId = 1,
                ShooterX = 0, ShooterY = 0, ShooterZ = 10, // 타겟 방향으로 에임
                AimX = 1, AimY = 0, AimZ = 0 // X축 양의 방향
            };

            var result = compensator.ValidateHit(request, 200);

            Assert.True(result.IsHit);
            Assert.Equal(8, result.RewindTick);
            Assert.Equal(0, result.Distance); // 정면 에임이므로 거리는 0
        }

        [Fact]
        public void LagCompensator_ShouldMissIfAimIsOff()
        {
            var buffer = new SnapshotBuffer(100);
            var mockLoop = new Mock<IGameLoop>();
            mockLoop.Setup(l => l.TickRate).Returns(20);
            mockLoop.Setup(l => l.CurrentTick).Returns(10);

            var state = new WorldState { TickNumber = 8 };
            state.Entities.TryAdd(1, new GameEntity { EntityId = 1, X = 10, Y = 0, Z = 10, HitboxRadius = 1.0f });
            buffer.Push(state);

            var compensator = new LagCompensator(buffer, mockLoop.Object);

            var request = new HitValidationRequest
            {
                TargetEntityId = 1,
                ShooterX = 0, ShooterY = 0, ShooterZ = 10,
                AimX = 1, AimY = 1, AimZ = 1 // 엉뚱한 방향
            };

            var result = compensator.ValidateHit(request, 200);

            Assert.False(result.IsHit);
        }
    }
}
