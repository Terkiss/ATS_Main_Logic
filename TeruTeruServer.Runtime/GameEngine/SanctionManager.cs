using System;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 보안 위반 이벤트를 분석하여 적절한 제재(Sanction)를 가하는 클래스입니다.
    /// </summary>
    public class SanctionManager
    {
        private readonly ISecurityEventLogger _logger;
        private readonly ISessionManager _sessionManager;
        
        // 제재 임계치 (L318-320 지시사항 준수)
        private const int WARNING_THRESHOLD = 3;
        private const int TEMP_BAN_THRESHOLD = 7;
        private const int PERMA_BAN_THRESHOLD = 15;
        
        public SanctionManager(ISecurityEventLogger logger, ISessionManager sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
        }
        
        /// <summary>
        /// SecurityEvent를 처리하고, 위반 횟수에 따라 제재 수준을 결정합니다.
        /// </summary>
        public int ProcessViolation(ClientSession session, SecurityEvent evt)
        {
            _logger.LogEvent(evt);
            session.ViolationCount++;
            session.LastViolationUtc = DateTime.UtcNow;
            
            if (session.ViolationCount >= PERMA_BAN_THRESHOLD)
            {
                session.BanLevel = 3; // 영구 차단
                TeruTeruLogger.LogWarning($"[Sanction] HostID {session.HostID} PERMANENTLY BANNED ({session.ViolationCount} violations)");
            }
            else if (session.ViolationCount >= TEMP_BAN_THRESHOLD)
            {
                session.BanLevel = 2; // 임시 차단
                TeruTeruLogger.LogWarning($"[Sanction] HostID {session.HostID} TEMP BANNED ({session.ViolationCount} violations)");
            }
            else if (session.ViolationCount >= WARNING_THRESHOLD)
            {
                session.BanLevel = 1; // 경고
                TeruTeruLogger.LogWarning($"[Sanction] HostID {session.HostID} WARNING ({session.ViolationCount} violations)");
            }
            
            return session.BanLevel;
        }
    }
}
