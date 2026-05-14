using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    public class MatchQueue
    {
        private readonly ConcurrentQueue<MatchEntry> _queue = new();
        private readonly IGameSessionManager _sessionManager;
        private readonly int _teamSize;
        private readonly int _teamCount;
        private readonly int _initialMmrRange;
        private int _currentMmrRange;
        private DateTime _lastRangeExpansionUtc = DateTime.UtcNow;

        public MatchQueue(IGameSessionManager sessionManager, int teamSize = 4, int teamCount = 2, int mmrRange = 200)
        {
            _sessionManager = sessionManager;
            _teamSize = teamSize;
            _teamCount = teamCount;
            _initialMmrRange = mmrRange;
            _currentMmrRange = mmrRange;
        }

        public void Enqueue(MatchEntry entry)
        {
            _queue.Enqueue(entry);
            TeruTeruLogger.LogInfo($"[MatchQueue] Player {entry.HostId} enqueued. (MMR: {entry.Mmr})");
        }

        public bool Dequeue(int hostId)
        {
            lock (_queue)
            {
                var list = _queue.ToList();
                int originalCount = list.Count;
                var filtered = list.Where(p => p.HostId != hostId).ToList();
                
                if (filtered.Count < originalCount)
                {
                    while (_queue.TryDequeue(out _)) ;
                    foreach (var entry in filtered) _queue.Enqueue(entry);
                     TeruTeruLogger.LogInfo($"[MatchQueue] Player {hostId} dequeued.");
                    return true;
                }
            }
            return false;
        }

        public int QueueLength => _queue.Count;

        /// <summary>
        /// Tick 핸들러에서 호출. 매칭 조건이 맞으면 GameSession 생성.
        /// </summary>
        public void TryMatch()
        {
            if (_queue.Count < _teamSize * _teamCount) return;

            // MMR 범위 자동 확대 (30초마다 +100)
            if ((DateTime.UtcNow - _lastRangeExpansionUtc).TotalSeconds >= 30)
            {
                _currentMmrRange += 100;
                _lastRangeExpansionUtc = DateTime.UtcNow;
                TeruTeruLogger.LogInfo($"[MatchQueue] MMR range expanded to {_currentMmrRange}.");
            }

            lock (_queue)
            {
                var allEntries = _queue.ToList();
                if (allEntries.Count < _teamSize * _teamCount) return;

                // MMR 순으로 정렬하여 근접한 플레이어끼리 묶음
                var sorted = allEntries.OrderBy(p => p.Mmr).ToList();
                
                int requiredCount = _teamSize * _teamCount;
                for (int i = 0; i <= sorted.Count - requiredCount; i++)
                {
                    var candidate = sorted.Skip(i).Take(requiredCount).ToList();
                    int minMmr = candidate.Min(p => p.Mmr);
                    int maxMmr = candidate.Max(p => p.Mmr);

                    if (maxMmr - minMmr <= _currentMmrRange)
                    {
                        // 매칭 성공
                        _sessionManager.CreateSession(candidate, _teamCount);
                        
                        // 큐에서 제거
                        var matchedIds = candidate.Select(p => p.HostId).ToHashSet();
                        var remaining = allEntries.Where(p => !matchedIds.Contains(p.HostId)).ToList();
                        
                        while (_queue.TryDequeue(out _)) ;
                        foreach (var entry in remaining) _queue.Enqueue(entry);
                        
                        // 범위 초기화
                        _currentMmrRange = _initialMmrRange; 
                        _lastRangeExpansionUtc = DateTime.UtcNow;
                        
                        TeruTeruLogger.LogInfo($"[MatchQueue] Match found for {requiredCount} players.");
                        return; // 한 번에 한 세션만 생성 (다음 Tick에 이어서)
                    }
                }
            }
        }
    }
}
