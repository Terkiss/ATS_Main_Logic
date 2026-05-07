using System;
using System.Net.Sockets;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Logic.Default.P2P
{
    public class P2PRelayHandler
    {
        private readonly ISessionManager _sessionManager;

        public P2PRelayHandler(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        // P2PRelayProtocol 구조 (Direct Bytes):
        public void HandleRelayData(byte[] rawData)
        {
            try
            {
                var buffer = rawData;
                if (buffer.Length < 6) return;

                int targetHostId = BitConverter.ToInt32(buffer, 6);

                if (_sessionManager.Players.TryGetValue(targetHostId, out var targetSession))
                {
                    if (targetSession.State == SessionState.Connected && targetSession.ClientSocket != null)
                    {
                        // 원본 패킷 그대로 전달 (클라이언트에서 발신자 구분이 필요할 경우 헤더에 발신자 ID를 추가해야 함)
                        // 현재는 단순 포워딩만 수행
                        targetSession.ClientSocket.SendAsync(new ReadOnlyMemory<byte>(buffer), SocketFlags.None);
                    }
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"Relay 중 에러 발생: {ex.Message}");
            }
        }
    }
}
