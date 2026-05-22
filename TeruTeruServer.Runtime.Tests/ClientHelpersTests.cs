using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;
using Moq;
using TeruTeruServer.Client;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Protocol;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class ClientHelpersTests
    {
        [Fact]
        public void SnapshotInterpolator_ShouldBufferAndInterpolateCorrectly()
        {
            // Arrange
            var interpolator = new SnapshotInterpolator(maxBufferSize: 3);
            var now = DateTime.UtcNow;

            var s1 = new WorldState { TickNumber = 1, Timestamp = now.AddMilliseconds(-100) };
            s1.Entities.TryAdd(10, new GameEntity { EntityId = 10, X = 10f, Y = 0f, Z = 10f, RotationY = 350f });

            var s2 = new WorldState { TickNumber = 2, Timestamp = now };
            s2.Entities.TryAdd(10, new GameEntity { EntityId = 10, X = 20f, Y = 0f, Z = 20f, RotationY = 10f }); // Rotation wraps across 360/0

            var s3 = new WorldState { TickNumber = 3, Timestamp = now.AddMilliseconds(100) };
            s3.Entities.TryAdd(10, new GameEntity { EntityId = 10, X = 30f, Y = 0f, Z = 30f, RotationY = 30f });

            // Act & Assert (Limit buffer size test)
            interpolator.AddSnapshot(s1);
            interpolator.AddSnapshot(s2);
            interpolator.AddSnapshot(s3);

            // Add one more to verify s1 is evicted (buffer limit 3)
            var s4 = new WorldState { TickNumber = 4, Timestamp = now.AddMilliseconds(200) };
            s4.Entities.TryAdd(10, new GameEntity { EntityId = 10, X = 40f, Y = 0f, Z = 40f, RotationY = 50f });
            interpolator.AddSnapshot(s4);

            // Target time is exactly half-way between s2 and s3 (now + 50ms)
            // RenderTime = now + 150ms, Delay = 100ms => targetTime = now + 50ms
            var interpolated = interpolator.Interpolate(now.AddMilliseconds(150), 100);

            Assert.NotNull(interpolated);
            Assert.True(interpolated.Entities.TryGetValue(10, out var ent));
            
            // X and Z should be 25f (midway between 20f and 30f)
            Assert.Equal(25f, ent.X, precision: 3);
            Assert.Equal(25f, ent.Z, precision: 3);

            // Target time is exactly half-way between s2 and s3 (now - 50ms is evicted since s1 was evicted)
            // Let's test interpolation between s2 (RotationY = 10) and s3 (RotationY = 30) => midway is 20
            Assert.Equal(20f, ent.RotationY, precision: 3);

            // Test angle wrapping interpolation: between s1 and s2 (if s1 wasn't evicted, but since it is, let's create a new interpolator)
            var interpolatorWrap = new SnapshotInterpolator();
            interpolatorWrap.AddSnapshot(s1); // RotationY = 350
            interpolatorWrap.AddSnapshot(s2); // RotationY = 10
            
            // Target time is exactly midway between s1 and s2 (now - 50ms)
            var wrapInterpolated = interpolatorWrap.Interpolate(now.AddMilliseconds(50), 100);
            Assert.NotNull(wrapInterpolated);
            Assert.True(wrapInterpolated.Entities.TryGetValue(10, out var wrapEnt));

            // Shortest path between 350 and 10 is 20 degrees (+10 degrees from 350 or -10 from 10)
            // Midway rotation should be 0f (or 360f)
            float expectedRot = wrapEnt.RotationY;
            Assert.True(Math.Abs(expectedRot - 0f) < 0.1f || Math.Abs(expectedRot - 360f) < 0.1f);
        }

        [Fact]
        public void ClientPredictionBuffer_Reconciliation_ShouldCorrectAndReplay()
        {
            // Arrange
            var buffer = new ClientPredictionBuffer();
            var simulationDelegate = new Action<GameEntity, GameInput>((entity, input) =>
            {
                // Simple physics: MoveX/Z updates position
                entity.X += input.MoveX;
                entity.Z += input.MoveZ;
            });

            // Initial predicted state at tick 10
            var ent10 = new GameEntity { EntityId = 10, X = 1f, Z = 1f };
            var input10 = new GameInput { ClientTick = 10, MoveX = 1f, MoveZ = 1f };
            buffer.RecordInput(input10, ent10); // Predicted position will be 1, 1

            // Predicted state at tick 11 (after applying input11)
            var ent11 = new GameEntity { EntityId = 10, X = 2f, Z = 2f };
            var input11 = new GameInput { ClientTick = 11, MoveX = 1f, MoveZ = 1f };
            buffer.RecordInput(input11, ent11); // Predicted position will be 2, 2

            // Predicted state at tick 12
            var ent12 = new GameEntity { EntityId = 10, X = 3f, Z = 3f };
            var input12 = new GameInput { ClientTick = 12, MoveX = 1f, MoveZ = 1f };
            buffer.RecordInput(input12, ent12); // Predicted position will be 3, 3

            // Act: Server sends acknowledgement for tick 11, but server says position at tick 11 was (5f, 5f) instead of (2f, 2f)
            var ack = new StateAck
            {
                LastProcessedClientTick = 11,
                X = 5f,
                Y = 0f,
                Z = 5f,
                VelocityX = 0f,
                VelocityZ = 0f
            };

            // Reconciliation should snap tick 11 to (5, 5) and replay input 12 (MoveX=1, MoveZ=1) to yield (6, 6)
            var finalState = buffer.Reconcile(ack, simulationDelegate, epsilon: 0.1f);

            // Assert
            Assert.NotNull(finalState);
            Assert.Equal(6f, finalState.X);
            Assert.Equal(6f, finalState.Z);

            // Ticks 10 and 11 should have been cleaned up. Only tick 12's successor or final state remains.
            // Since tick 12 was the last unacknowledged input, the buffer count should be 1 (for tick 12).
            Assert.Equal(1, buffer.BufferCount);
        }

        [Fact]
        public void ClientPredictionBuffer_NoReconciliation_WhenWithinEpsilon()
        {
            // Arrange
            var buffer = new ClientPredictionBuffer();
            var simulationDelegate = new Action<GameEntity, GameInput>((entity, input) => { });

            var ent = new GameEntity { EntityId = 10, X = 1.0001f, Z = 1f };
            var input = new GameInput { ClientTick = 10 };
            buffer.RecordInput(input, ent);

            // Server ack with very close coordinates (difference is 0.0001f, which is within 0.001f epsilon)
            var ack = new StateAck
            {
                LastProcessedClientTick = 10,
                X = 1.0f,
                Z = 1.0f
            };

            // Act
            var finalState = buffer.Reconcile(ack, simulationDelegate, epsilon: 0.001f);

            // Assert
            Assert.NotNull(finalState);
            // Position should NOT have snapped to server (retained 1.0001f) because error is within epsilon
            Assert.Equal(1.0001f, finalState.X);
            Assert.Equal(0, buffer.BufferCount); // Buffer cleared since tick 10 <= 10
        }

        [Fact]
        public void TeruClient_HighLevelHelpers_ShouldInitializeDefaultValues()
        {
            // Arrange
            var client = new TeruClient("127.0.0.1", 9999);

            // Assert default values
            Assert.Equal(0, client.HostId);
            Assert.Null(client.ReconnectToken);
            Assert.Equal(0, client.CurrentZoneId);
            Assert.Null(client.CurrentRoomId);

            // Check P2P exposed wrapper
            Assert.NotNull(client.P2P);
        }
    }
}
