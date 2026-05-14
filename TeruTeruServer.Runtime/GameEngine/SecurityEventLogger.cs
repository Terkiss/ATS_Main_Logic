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
    /// 보안 이벤트를 메모리에 기록하고 관리하는 클래스입니다.
    /// 세션당 최대 100개의 이벤트를 유지하며 파일 로그로도 기록합니다.
    /// </summary>
    public class SecurityEventLogger : ISecurityEventLogger
    {
        private readonly ConcurrentDictionary<int, List<SecurityEvent>> _events = new();
        private const int MAX_EVENTS_PER_SESSION = 100;

        public void LogEvent(SecurityEvent evt)
        {
            var list = _events.GetOrAdd(evt.HostId, _ => new List<SecurityEvent>());
            lock (list)
            {
                list.Add(evt);
                // 메모리 관리: 최대 보관 개수 초과 시 오래된 것 삭제 (L455 주의사항)
                if (list.Count > MAX_EVENTS_PER_SESSION)
                {
                    list.RemoveAt(0);
                }
            }

            // 파일 로그도 기록
            TeruTeruLogger.LogWarning($"[Security] {evt.EventType} | HostID: {evt.HostId} | {evt.Description} | Severity: {evt.Severity}");
        }

        public IReadOnlyList<SecurityEvent> GetRecentEvents(int hostId, int count = 10)
        {
            if (_events.TryGetValue(hostId, out var list))
            {
                lock (list)
                {
                    return list.TakeLast(count).ToList();
                }
            }
            return Array.Empty<SecurityEvent>();
        }

        public int GetViolationCount(int hostId)
        {
            if (_events.TryGetValue(hostId, out var list))
            {
                lock (list)
                {
                    return list.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// 세션 종료 시 호출하여 메모리를 정리합니다. (L455 주의사항)
        /// </summary>
        public void RemoveSession(int hostId)
        {
            _events.TryRemove(hostId, out _);
        }
    }
}
