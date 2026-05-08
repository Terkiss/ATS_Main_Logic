using System;
using System.Threading.Tasks;
using TeruTeruServer.Client;
using TeruTeruServer.SDK.Attributes;

namespace DummyClient
{
    // 사용자는 이렇게만 짜면 통신이 되어야 함
    // [SDK Template Example] 외부 개발자는 이렇게 클래스만 정의하고 어트리뷰트를 붙여서 비즈니스 로직을 구현합니다.
    public class MyClientLogic 
    {
        // 1. [Rpc] 어트리뷰트: 서버가 호출하는 메서드 (문자열 매핑)
        [Rpc("OnServerMessage")]
        public void OnMessage(string msg) 
        {
            Console.WriteLine($"[MyClientLogic] Server Says: {msg}");
        }

        // 2. [Protocol] 어트리뷰트: 서버가 특정 Enum 프로토콜로 보내는 데이터 처리
        [Protocol(TeruTeruServer.SDK.Enums.ProtocolSelect.QueueCountCommand)]
        public void OnQueueCountUpdate(TeruTeruServer.SDK.Protocol.YoloDetectResult result)
        {
            Console.WriteLine($"[MyClientLogic] Queue Count Update / Detection: {result.DetectionResult}");
        }

        // 3. 재연결 성공 시 서버가 보내는 알림 처리
        [Protocol(TeruTeruServer.SDK.Enums.ProtocolSelect.ReconnectProtocol)]
        public void OnReconnectSuccess(object payload)
        {
            Console.WriteLine("[MyClientLogic] Reconnection fallback success notification received.");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== [TeruTeruServer SDK Finalization Reference] ===");
            
            // TeruClient 생성 (IP, Port)
            using var client = new TeruClient("127.0.0.1", 3000);
            
            // 1. 비즈니스 로직 객체 등록 (어트리뷰트 기반 자동 라우팅)
            client.RegisterLogic(new MyClientLogic());

            // 로그 및 이벤트 핸들러 연결
            client.OnLog += Console.WriteLine;
            client.OnDisconnected += () => Console.WriteLine("[Event] Disconnected from server.");

            // 2. 서버 연결
            if (!await client.ConnectAsync())
            {
                Console.WriteLine("Failed to connect. Exit.");
                return;
            }

            // 3. 로그인 (내부적으로 JWT 발급 및 P2P(UDP) 통신 초기화 자동 수행)
            if (await client.LoginAsync("developer_test", "dev_pass"))
            {
                Console.WriteLine("[Step 1] Login successful.");

                // 4. 고수준 RPC 호출 예제
                Console.WriteLine("[Step 2] Testing RPC 'Echo'...");
                var echoResult = await client.InvokeRpcAsync<string>("Echo", "Hello Developer!");
                Console.WriteLine($"RPC Response: {echoResult}");

                // 5. P2P 및 그룹 통신 예제 (Milestone 5 연동)
                Console.WriteLine("[Step 3] Joining P2P Group 101...");
                // Note: 현재 SDK에서는 SendProtocolAsync를 통해 명시적 조인 요청 가능
                await client.SendJsonAsync(TeruTeruServer.SDK.Enums.ProtocolSelect.JoinGroupProtocol, new { GroupID = 101 });

                // 6. 재연결(Reconnect) 테스트 예시 (설명용)
                // 만약 연결이 끊겼다면:
                // bool reconnected = await client.ReconnectAsync(savedHostId, savedReconnectToken);

                Console.WriteLine("\n[Ready] SDK client is now listening for server-side pushes.");
            }
            else
            {
                Console.WriteLine("[Error] Login failed.");
            }

            Console.WriteLine("\nPress any key to keep client alive (or 'q' to exit).");
            while (Console.ReadKey().KeyChar != 'q') ;
        }
    }
}
