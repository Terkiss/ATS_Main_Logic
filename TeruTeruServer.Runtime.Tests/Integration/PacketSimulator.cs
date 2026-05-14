using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TeruTeruServer.Runtime.Testing;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.Runtime.Tests.Integration
{
    /// <summary>
    /// MockServer를 사용하여 패킷 전송 및 응답 수신을 시뮬레이션하는 유틸리티입니다.
    /// </summary>
    public class PacketSimulator
    {
        private readonly MockServer _server;

        public PacketSimulator(MockServer server)
        {
            _server = server;
        }

        /// <summary>
        /// JSON 페이로드를 보내고 첫 번째 응답을 특정 타입으로 디시리얼라이즈하여 반환합니다.
        /// </summary>
        public async Task<T?> SendAndReceive<T>(ProtocolSelect protocol, object request) where T : class
        {
            var responses = await _server.ProcessJsonAsync(protocol, request);
            if (responses == null || responses.Count == 0) 
            {
                System.Console.WriteLine($"[Simulator] No response received for protocol {protocol}");
                return null;
            }

            // 헤더(6바이트)를 제외한 페이로드 추출
            byte[] responsePacket = responses[0];
            if (responsePacket.Length < 6) 
            {
                 System.Console.WriteLine($"[Simulator] Response too short: {responsePacket.Length}");
                 return null;
            }

            string json = System.Text.Encoding.UTF8.GetString(responsePacket, 6, responsePacket.Length - 6);
            System.Console.WriteLine($"[Simulator] Raw Response JSON: {json}");

            try 
            {
                if (typeof(T) == typeof(object) || typeof(T).Name == "Object" || typeof(T).Name.Contains("JsonElement"))
                {
                    return JsonSerializer.Deserialize<JsonElement>(json) as T;
                }
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[Simulator] Deserialization Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 원본 바이트 패킷을 전송하고 모든 응답 바이트 목록을 반환합니다.
        /// </summary>
        public async Task<List<byte[]>> SendRaw(byte[] packet)
        {
            return await _server.ProcessPacketAsync(packet);
        }
    }
}
