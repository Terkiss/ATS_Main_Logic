using System;
using System.Threading.Tasks;
using TeruTeruServer.Client;
using TeruTeruServer.SDK.Attributes;

namespace DummyClient
{
    // 사용자는 이렇게만 짜면 통신이 되어야 함
    public class MyClientLogic 
    {
        [Rpc("OnServerMessage")]
        public void OnMessage(string msg) 
        {
            Console.WriteLine($"[MyClientLogic] Server Says: {msg}");
        }

        [Protocol(TeruTeruServer.SDK.Enums.ProtocolSelect.QueueCountCommand)]
        public void OnQueueCountUpdate(TeruTeruServer.SDK.Protocol.YoloDetectResult result)
        {
            Console.WriteLine($"[MyClientLogic] Queue Count Update / Detection: {result.DetectionResult}");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== [High-Level SDK Test] TeruTeruServer Integration ===");
            
            using var client = new TeruClient("127.0.0.1", 3000);
            
            // 1. 객체 등록 (어트리뷰트 기반 라우팅 활성화)
            client.RegisterLogic(new MyClientLogic());

            // 로그 이벤트 연결
            client.OnLog += Console.WriteLine;
            client.OnDisconnected += () => Console.WriteLine("[Event] Disconnected from server.");

            // 2. 서버 연결
            if (!await client.ConnectAsync())
            {
                Console.WriteLine("Failed to connect. Exit.");
                return;
            }

            // 3. 로그인 (내부적으로 JWT 발급 및 P2P(UDP) 시작 자동화)
            if (await client.LoginAsync("testuser", "testpassword"))
            {
                // 4. 추상화된 Rpc 전송 테스트
                Console.WriteLine("Sending RPC 'Echo' to server...");
                await client.InvokeRpcAsync("Echo", "Hello Server");

                Console.WriteLine("\n[Success] All high-level SDK sequences initiated.");
            }
            else
            {
                Console.WriteLine("[Error] Login failed.");
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
