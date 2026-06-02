using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Logic.Default.P2P
{
    public static class SocketExtensions
    {
        /// <summary>
        /// 소켓을 통해 데이터를 JSON 형식으로 직렬화하여 패킷 헤더와 함께 전송합니다.
        /// </summary>
        public static void SendJsonResponse<T>(this Socket socket, ProtocolSelect protocol, T data)
        {
            if (socket == null || !socket.Connected) return;
            try
            {
                string json = JsonSerializer.Serialize(data);
                byte[] body = Encoding.UTF8.GetBytes(json);
                byte[] packet = new byte[body.Length + 6];
                packet[0] = (byte)SendType.Json;
                packet[1] = (byte)protocol;
                // SequenceNumber (2-5) remains 0
                Array.Copy(body, 0, packet, 6, body.Length);
                socket.Send(packet);
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"[SocketExtensions] SendJsonResponse 실패 (Protocol: {protocol}): {ex.Message}");
            }
        }
    }
}
