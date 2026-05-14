using System;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 클라이언트별 패킷 요청 속도를 제한하여 DoS 공격(플러딩)을 방어하는 미들웨어입니다.
    /// </summary>
    public class RateLimitMiddleware : IPacketMiddleware
    {
        private readonly int _maxPacketsPerSecond;

        public RateLimitMiddleware(int maxPacketsPerSecond = 50)
        {
            _maxPacketsPerSecond = maxPacketsPerSecond;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            if (context.Session != null)
            {
                var now = DateTime.UtcNow;
                if ((now - context.Session.LastPacketTime).TotalSeconds >= 1)
                {
                    context.Session.LastPacketTime = now;
                    context.Session.CurrentSecondPacketCount = 1;
                }
                else
                {
                    context.Session.CurrentSecondPacketCount++;
                    if (context.Session.CurrentSecondPacketCount > _maxPacketsPerSecond)
                    {
                        // 설정된 임계치 초과 시 경고를 기록하고 패킷을 버림(Drop)
                        TeruTeruLogger.LogWarning($"[RateLimit] HostID {context.Session.HostID} exceeded packet limit ({_maxPacketsPerSecond}/s). Dropping packet.");
                        context.IsProcessed = true; // 이후 미들웨어 및 로직 실행 취소
                        return;
                    }
                }
            }

            await next();
        }
    }
}
