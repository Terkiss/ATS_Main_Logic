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
            const double targetFrameTimeMs = 1000.0 / tickRate;
            var loop = new GameLoop(tickRate);
            
            var stopwatch = new Stopwatch();
            var tickTimes = new List<double>();
            var lockObj = new object();
            
            loop.RegisterTickHandler(tick => {
                lock (lockObj)
                {
                    if (!stopwatch.IsRunning)
                    {
                        stopwatch.Start();
                    }
                    tickTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                }
            });

            loop.Start();
            await Task.Delay(300); // 300ms 대기로 틱이 충분히 발생할 여유 제공
            loop.Stop();

            int callCount;
            List<double> localTickTimes;
            lock (lockObj)
            {
                callCount = tickTimes.Count;
                localTickTimes = new List<double>(tickTimes);
            }

            // 최소 3회 이상 틱이 발생했는지 검증 (시작 지연을 감안해도 300ms 대기 시 반드시 만족)
            Assert.True(callCount >= 3, $"Actual call count: {callCount}");

            // 첫 번째 틱과 마지막 틱 사이의 실제 경과 시간
            double actualElapsed = localTickTimes[callCount - 1] - localTickTimes[0];
            
            // 틱 횟수에 따른 기대 경과 시간 (예: 5회 틱인 경우 첫 틱 기준 4개 프레임이 흘렀어야 함 -> 4 * 50ms = 200ms)
            double expectedElapsed = (callCount - 1) * targetFrameTimeMs;

            // 허용 오차 (OS의 타이머 분해능 및 스레드 대기 편차를 고려하여 1.5 틱 크기 정도로 설정)
            double tolerance = targetFrameTimeMs * 1.5;

            Assert.True(Math.Abs(actualElapsed - expectedElapsed) < tolerance,
                $"Expected elapsed time for {callCount} ticks was {expectedElapsed}ms, but actual was {actualElapsed}ms (Diff: {Math.Abs(actualElapsed - expectedElapsed)}ms).");
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
