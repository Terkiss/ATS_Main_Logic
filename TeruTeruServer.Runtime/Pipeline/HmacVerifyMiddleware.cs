using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.Runtime.GameEngine;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 패킷의 HMAC 서명을 검증하여 데이터 변조를 방지하는 미들웨어입니다.
    /// </summary>
    public class HmacVerifyMiddleware : IPacketMiddleware
    {
        private readonly byte[] _serverKey;
        private readonly SanctionManager _sanctionManager;

        public HmacVerifyMiddleware(byte[] serverKey, SanctionManager sanctionManager)
        {
            _serverKey = serverKey;
            _sanctionManager = sanctionManager;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            // ★ HMAC bypass 조건: 인증 전 패킷은 HMAC가 없음 (L241, L447)
            if (context.Session == null || !context.Session.IsAuthenticated)
            {
                await next();
                return;
            }

            // 패킷 구조: [SendType(1)][Protocol(1)][SeqNum(4)][HMAC(32)][Payload...]
            // 최소 길이: 1+1+4+32 = 38
            if (context.RawData.Length >= 38)
            {
                byte[] receivedHmac = new byte[32];
                Array.Copy(context.RawData, 6, receivedHmac, 0, 32);

                // HMAC 계산 대상: [SendType(1)][Protocol(1)][SeqNum(4)] + [Payload(나머지)]
                byte[] dataToVerify = new byte[6 + (context.RawData.Length - 38)];
                Array.Copy(context.RawData, 0, dataToVerify, 0, 6);
                if (context.RawData.Length > 38)
                    Array.Copy(context.RawData, 38, dataToVerify, 6, context.RawData.Length - 38);

                using var hmac = new HMACSHA256(_serverKey);
                byte[] computedHmac = hmac.ComputeHash(dataToVerify);

                if (!CryptographicEquals(receivedHmac, computedHmac))
                {
                    TeruTeruLogger.LogWarning($"[HMAC] HostID {context.Session.HostID} packet tampered. Disconnecting.");
                    
                    // SecurityEvent 발행 (L213)
                    _sanctionManager.ProcessViolation(context.Session, new SecurityEvent
                    {
                        HostId = context.Session.HostID,
                        EventType = "PacketTamper",
                        Description = "HMAC signature mismatch - Packet may have been tampered",
                        Severity = "Critical"
                    });

                    context.IsProcessed = true;
                    return;
                }

                // HMAC를 제거한 깨끗한 패킷으로 교체
                byte[] cleanData = new byte[context.RawData.Length - 32];
                Array.Copy(context.RawData, 0, cleanData, 0, 6);
                if (context.RawData.Length > 38)
                    Array.Copy(context.RawData, 38, cleanData, 6, context.RawData.Length - 38);
                context.RawData = cleanData;
            }

            await next();
        }

        private static bool CryptographicEquals(byte[] a, byte[] b)
        {
            // 타이밍 공격 방지를 위한 고정 시간 비교 (L228, L453)
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
