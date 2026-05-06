using TeruTeruServer.SDK.Interfaces;
using System;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 수신된 패킷의 최소 길이나 형식을 검증하는 미들웨어입니다.
    /// </summary>
    public class ValidationMiddleware : IPacketMiddleware
    {
        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            if (context.RawData == null || context.RawData.Length < 1)
            {
                TeruTeruLogger.LogWarning("Invalid Packet: Data is null or too short.");
                return; // 처리 중단
            }

            // 추가적인 구조 검증 로직이 들어갈 자리

            await next();
        }
    }

    /// <summary>
    /// 암호화된 패킷을 복호화하는 미들웨어입니다. (Placeholder)
    /// </summary>
    public class DecryptionMiddleware : IPacketMiddleware
    {
        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            // TODO: 패킷 헤더 등을 확인하여 암호화 여부 판단 및 복호화 수행
            // context.RawData = Encrypt.DecryptStringAES(...);

            await next();
        }
    }
}
