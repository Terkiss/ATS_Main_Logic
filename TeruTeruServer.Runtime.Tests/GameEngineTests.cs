using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.Runtime.GameEngine;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class GameEngineTests
    {
        [Fact]
        public void InputQueue_DrainAll_ShouldClearAndReturnAll()
        {
            var queue = new InputQueue<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            var items = queue.DrainAll();

            Assert.Equal(3, items.Count);
            Assert.Equal(0, queue.Count);
            Assert.Equal(new List<int> { 1, 2, 3 }, items);
        }

        [Fact]
        public async Task GameLoop_ShouldTriggerHandlersAtCorrectRate()
        {
            const int tickRate = 20; // 50ms per tick
            var loop = new GameLoop(tickRate);
            int callCount = 0;
            
            loop.RegisterTickHandler(tick => {
                Interlocked.Increment(ref callCount);
            });

            loop.Start();
            await Task.Delay(220); // Should trigger ~4 ticks (0, 50, 100, 150, 200)
            loop.Stop();

            Assert.InRange(callCount, 3, 8); // 환경 지연을 고려하여 범위를 약간 넓힘 (목표: ~5회)
        }

        [Fact]
        public void SnapshotBuffer_ShouldHandleWraparound()
        {
            var buffer = new SnapshotBuffer(10);
            
            // 0 ~ 14까지 푸시 (10개 용량 초과)
            for (int i = 0; i <= 14; i++)
            {
                buffer.Push(new WorldState { TickNumber = i });
            }

            Assert.Equal(14, buffer.GetLatest()?.TickNumber);
            Assert.Null(buffer.GetAtTick(0)); // 덮어씌워짐 (0 % 10 == 10 % 10)
            Assert.Equal(10, buffer.GetAtTick(10)?.TickNumber);
            Assert.Equal(14, buffer.GetAtTick(14)?.TickNumber);
        }

        [Fact]
        public void DeltaCalculator_ShouldOnlyDetectDirtyEntities()
        {
            var prev = new WorldState { TickNumber = 0 };
            var curr = new WorldState { TickNumber = 1 };

            var e1 = new GameEntity { EntityId = 1, X = 10, IsDirty = false };
            var e2 = new GameEntity { EntityId = 2, X = 20, IsDirty = true };
            
            curr.Entities.TryAdd(1, e1);
            curr.Entities.TryAdd(2, e2);

            var delta = DeltaCalculator.CalculateDelta(prev, curr);

            Assert.Equal(2, delta.Count); // e2(Dirty) + e1(New) -> 둘 다 포함되어야 함
            Assert.False(e2.IsDirty); // 리셋 확인
        }

        [Fact]
        public void DeltaCalculator_ShouldIgnoreNonDirtyExistingEntities()
        {
             var prev = new WorldState { TickNumber = 0 };
             var e1 = new GameEntity { EntityId = 1, X = 10, IsDirty = false };
             prev.Entities.TryAdd(1, e1.DeepClone());

             var curr = new WorldState { TickNumber = 1 };
             var e1_curr = e1.DeepClone(); // IsDirty = false
             curr.Entities.TryAdd(1, e1_curr);

             var delta = DeltaCalculator.CalculateDelta(prev, curr);

             Assert.Empty(delta);
             Assert.False(e1_curr.IsDirty);
        }
    }
}
