using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeruTeruServer.Pipeline
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
            int index = 0;

            Func<Task> next = null;
            next = async () =>
            {
                if (index < _middlewares.Count)
                {
                    var middleware = _middlewares[index++];
                    await middleware.InvokeAsync(context, next);
                }
            };

            await next();
        }
    }
}
