using TeruTeruServer.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 미들웨어를 순차적으로 실행하는 파이프라인 관리자입니다.
    /// </summary>
    public class PacketPipeline
    {
        private readonly List<IPacketMiddleware> _middlewares = new List<IPacketMiddleware>();

        public void Use(IPacketMiddleware middleware)
        {
            _middlewares.Add(middleware);
        }

        public async Task ExecuteAsync(PacketContext context)
        {
            TeruTeruServer.SDK.Util.ServerMetrics.IncrementPacketCount();
            int index = 0;

            Func<Task> next = null;
            next = async () =>
            {
                if (index < _middlewares.Count)
                {
                    var middleware = _middlewares[index++];
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    
                    await middleware.InvokeAsync(context, next);
                    
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 50)
                    {
                        string hostId = context.Session != null ? context.Session.HostID.ToString() : "Unknown";
                        TeruTeruServer.SDK.Util.TeruTeruLogger.LogWarning($"[Profile] Middleware {middleware.GetType().Name} took {sw.ElapsedMilliseconds}ms for HostID {hostId}");
                    }
                }
            };

            await next();
        }
    }
}
