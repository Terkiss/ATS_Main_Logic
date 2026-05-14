using TeruTeruServer.SDK.Interfaces;
using System;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 수신된 패킷의 최소 길이나 형식을 검증하고 세션 정보를 컨텍스트에 설정하는 미들웨어입니다.
    /// </summary>
    public class ValidationMiddleware : IPacketMiddleware
    {
        private readonly ISessionManager _sessionManager;

        public ValidationMiddleware(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            if (context.RawData == null || context.RawData.Length < 1)
            {
                TeruTeruLogger.LogWarning("Invalid Packet: Data is null or too short.");
                return; // 처리 중단
            }

            // 세션 정보 자동 설정 (L49 주의사항 준수)
            if (context.Session == null && _sessionManager.TryGetHostIdBySocket(context.ClientSocket, out int hostId))
            {
                if (_sessionManager.Players.TryGetValue(hostId, out var session))
                {
                    context.Session = session;
                }
            }

            await next();
        }
    }

    /// <summary>
    /// 암호화된 패킷을 복호화하는 미들웨어입니다.
    /// </summary>
    public class DecryptionMiddleware : IPacketMiddleware
    {
        private readonly ICryptoService _cryptoService;

        public DecryptionMiddleware(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            // TODO: 패킷 헤더 등을 확인하여 암호화 여부 판단 및 복호화 수행
            // 예시: context.RawData = Encoding.UTF8.GetBytes(_cryptoService.Decrypt(Encoding.UTF8.GetString(context.RawData), "password"));

            await next();
        }
    }
}
