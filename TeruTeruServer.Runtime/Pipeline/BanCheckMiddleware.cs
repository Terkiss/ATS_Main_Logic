using System;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 차단된(Banned) 세션의 패킷을 최상단에서 차단하는 미들웨어입니다.
    /// </summary>
    public class BanCheckMiddleware : IPacketMiddleware
    {
        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            // 세션이 있고 BanLevel이 2(임시차단) 이상인 경우 패킷 드랍 (L363)
            if (context.Session != null && context.Session.BanLevel >= 2)
            {
                TeruTeruLogger.LogInfo($"[BanCheck] HostID {context.Session.HostID} is banned (Level {context.Session.BanLevel}). Dropping packet.");
                context.IsProcessed = true;
                return;
            }
            await next();
        }
    }
}
