using System.Collections.Generic;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 보안 위반 이벤트를 기록하고 조회하는 인터페이스입니다.
    /// </summary>
    public interface ISecurityEventLogger
    {
        void LogEvent(SecurityEvent evt);
        IReadOnlyList<SecurityEvent> GetRecentEvents(int hostId, int count = 10);
        int GetViolationCount(int hostId);
    }
}
