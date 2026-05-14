using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 고정된 주기로 틱을 발생시키는 게임 루프 구현체입니다.
    /// </summary>
    public class GameLoop : IGameLoop
    {
        public int TickRate { get; }
        public long CurrentTick { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly List<Action<long>> _tickHandlers = new();
        private readonly object _handlerLock = new();
        private Thread? _loopThread;
        private readonly double _targetFrameTimeMs;

        public GameLoop(int tickRate = 20)
        {
            TickRate = tickRate;
            _targetFrameTimeMs = 1000.0 / tickRate;
        }

        public void Start()
        {
            if (IsRunning) return;

            IsRunning = true;
            _loopThread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "GameLoopThread"
            };
            _loopThread.Start();
            TeruTeruLogger.LogInfo($"GameLoop started at {TickRate}Hz.");
        }

        public void Stop()
        {
            IsRunning = false;
            _loopThread?.Join(1000);
            TeruTeruLogger.LogInfo("GameLoop stopped.");
        }

        public void RegisterTickHandler(Action<long> handler)
        {
            lock (_handlerLock)
            {
                if (!_tickHandlers.Contains(handler))
                {
                    _tickHandlers.Add(handler);
                }
            }
        }

        public void UnregisterTickHandler(Action<long> handler)
        {
            lock (_handlerLock)
            {
                _tickHandlers.Remove(handler);
            }
        }

        private void Loop()
        {
            var sw = Stopwatch.StartNew();
            double nextTickTime = sw.Elapsed.TotalMilliseconds;

            while (IsRunning)
            {
                double currentTime = sw.Elapsed.TotalMilliseconds;

                if (currentTime >= nextTickTime)
                {
                    CurrentTick++;
                    ExecuteHandlers(CurrentTick);
                    nextTickTime += _targetFrameTimeMs;
                }

                // 정밀도 보정을 위한 대기 로직
                double sleepTime = nextTickTime - sw.Elapsed.TotalMilliseconds;
                if (sleepTime > 1.0)
                {
                    // 1ms 이상 남았으면 Sleep
                    Thread.Sleep((int)sleepTime);
                }
                else if (sleepTime > 0)
                {
                    // 1ms 이내로 남았으면 정밀 대기 (SpinWait)
                    Thread.SpinWait(10);
                }
            }
        }

        private void ExecuteHandlers(long tick)
        {
            Action<long>[] handlers;
            lock (_handlerLock)
            {
                handlers = _tickHandlers.ToArray();
            }

            foreach (var handler in handlers)
            {
                try
                {
                    handler.Invoke(tick);
                }
                catch (Exception ex)
                {
                    TeruTeruLogger.LogError($"Error in TickHandler at tick {tick}: {ex.Message}");
                }
            }
        }
    }
}
