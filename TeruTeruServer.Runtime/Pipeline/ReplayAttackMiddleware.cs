using System;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    /// <summary>
    /// 클라이언트가 보낸 SequenceNumber를 검증하여 Replay Attack을 방어하는 미들웨어입니다.
    /// </summary>
    public class ReplayAttackMiddleware : IPacketMiddleware
    {
        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            // 구조: [SendType(1)][ProtocolType(1)][SequenceNumber(4)]...
            if (buffer.Length >= 6 && context.Session != null)
            {
                uint sequenceNumber = BitConverter.ToUInt32(buffer, 2);

                if (sequenceNumber > 0)
                {
                    if (sequenceNumber <= context.Session.LastSequenceNumber)
                    {
                        TeruTeruLogger.LogWarning($"[ReplayAttack] HostID {context.Session.HostID} sent duplicate or old sequence number: {sequenceNumber}. Dropping packet.");
                        context.IsProcessed = true;
                        return;
                    }
                    else
                    {
                        context.Session.LastSequenceNumber = sequenceNumber;
                    }
                }
            }

            await next();
        }
    }
}
