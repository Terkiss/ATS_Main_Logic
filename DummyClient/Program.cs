using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Protocol;

namespace DummyClient
{
    class Program
    {
        private static string _jwtToken = "";
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 3000;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== [Dummy Client] Testing TeruTeruServer.SDK SDK Architecture ===");
            
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(ServerIp, ServerPort);
                Console.WriteLine($"[1] Connected to Server at {ServerIp}:{ServerPort}");

                // 1. Login 시도
                await SendLoginRequest(socket);
                
                // 2. 응답 대기 및 토큰 추출
                await ReceiveAndExtractToken(socket);

                if (!string.IsNullOrEmpty(_jwtToken))
                {
                    Console.WriteLine($"[3] Token Received: {_jwtToken.Substring(0, 15)}...");
                    
                    // 3. 인증된 패킷 전송 (Common Library의 Enum 사용)
                    await SendAuthenticatedRequest(socket);

                    // 4. 최종 응답 확인
                    await ReceiveFinalResponse(socket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }

            Console.WriteLine("\n=== Test Completed. Press any key to exit. ===");
            Console.ReadKey();
        }

        private static async Task SendLoginRequest(Socket socket)
        {
            var loginData = new LoginProtocol { UserId = "testuser", Password = "testpassword" };
            string json = JsonSerializer.Serialize(loginData);
            byte[] body = Encoding.UTF8.GetBytes(json);

            byte[] packet = new byte[body.Length + 2];
            packet[0] = (byte)SendType.Json;
            packet[1] = (byte)ProtocolSelect.LoginProtocol;
            Buffer.BlockCopy(body, 0, packet, 2, body.Length);

            await socket.SendAsync(packet, SocketFlags.None);
            Console.WriteLine("[2] Sent Login Request (No Token Header)");
        }

        private static async Task ReceiveAndExtractToken(Socket socket)
        {
            byte[] buffer = new byte[4096];
            int received = await socket.ReceiveAsync(buffer, SocketFlags.None);
            
            if (received > 2)
            {
                string json = Encoding.UTF8.GetString(buffer, 2, received - 2);
                var response = JsonSerializer.Deserialize<LoginProtocol>(json);
                if (response != null && !string.IsNullOrEmpty(response.AuthToken))
                {
                    _jwtToken = response.AuthToken;
                }
            }
        }

        private static async Task SendAuthenticatedRequest(Socket socket)
        {
            // [SendType(1)][ProtocolType(1)][TokenLength(4)][Token(N)][Data(M)]
            var requestData = new { Command = "QueueCount" };
            string json = JsonSerializer.Serialize(requestData);
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] tokenBytes = Encoding.UTF8.GetBytes(_jwtToken);
            byte[] tokenLenBytes = BitConverter.GetBytes(tokenBytes.Length);

            int totalLen = 2 + 4 + tokenBytes.Length + body.Length;
            byte[] packet = new byte[totalLen];

            packet[0] = (byte)SendType.Json;
            packet[1] = (byte)ProtocolSelect.QueueCountCommand;
            
            Buffer.BlockCopy(tokenLenBytes, 0, packet, 2, 4);
            Buffer.BlockCopy(tokenBytes, 0, packet, 6, tokenBytes.Length);
            Buffer.BlockCopy(body, 0, packet, 6 + tokenBytes.Length, body.Length);

            await socket.SendAsync(packet, SocketFlags.None);
            Console.WriteLine("[4] Sent Authenticated Request (Using Common SDK)");
        }

        private static async Task ReceiveFinalResponse(Socket socket)
        {
            byte[] buffer = new byte[4096];
            int received = await socket.ReceiveAsync(buffer, SocketFlags.None);
            Console.WriteLine($"[5] Final Server Response Received ({received} bytes)");
        }
    }
}
