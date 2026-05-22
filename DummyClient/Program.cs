using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeruTeruServer.Client;
using TeruTeruServer.SDK.Attributes;

namespace DummyClient
{
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
            Console.WriteLine("=== [TeruTeruServer Client SDK Verification & Test Client] ===");
            
            // 1. TeruClient 생성 (IP, Port)
            using var client = new TeruClient("127.0.0.1", 3000);
            
            // 비즈니스 로직 객체 등록 (어트리뷰트 기반 자동 라우팅)
            client.RegisterLogic(new MyClientLogic());

            // 로그 및 이벤트 핸들러 연결
            client.OnLog += msg => Console.WriteLine($"[Log] {msg}");
            client.OnDisconnected += () => Console.WriteLine("[Event] Disconnected from server.");

            // 2. 서버 연결
            Console.WriteLine("\n[1] Connecting to server...");
            if (!await client.ConnectAsync())
            {
                Console.WriteLine("Failed to connect. Make sure the server is running!");
                return;
            }

            // 3. 로그인 (내부적으로 JWT 발급 및 P2P(UDP) 통신 초기화 자동 수행)
            Console.WriteLine("\n[2] Logging in...");
            if (await client.LoginAsync("developer_test", "dev_pass"))
            {
                Console.WriteLine($"[Login Success] HostId: {client.HostId}, ReconnectToken: {client.ReconnectToken}");

                // 4. 고수준 RPC 호출 예제
                Console.WriteLine("\n[3] Testing RPC 'Echo'...");
                var echoResult = await client.InvokeRpcAsync<string>("Echo", "Hello Developer!");
                Console.WriteLine($"RPC Response: {echoResult}");

                // 5. Zone Transfer (존 이동) 테스트
                Console.WriteLine("\n[4] Joining Zone 1...");
                bool joinedZone = await client.JoinZoneAsync(1);
                Console.WriteLine($"Joined Zone 1 result: {joinedZone}. CurrentZoneId: {client.CurrentZoneId}");

                // 6. P2P 및 룸 입장 (Room Join) 테스트
                Console.WriteLine("\n[5] Entering Room 101...");
                bool joinedRoom = await client.EnterRoomAsync(101);
                Console.WriteLine($"Entered Room 101 result: {joinedRoom}. CurrentRoomId: {client.CurrentRoomId}");

                // 7. SnapshotInterpolator 데모
                Console.WriteLine("\n[6] Testing SnapshotInterpolator...");
                var interpolator = new SnapshotInterpolator();

                var ws1 = new TeruTeruServer.SDK.GameEngine.WorldState
                {
                    TickNumber = 1,
                    Timestamp = DateTime.UtcNow
                };
                ws1.Entities.TryAdd(1, new TeruTeruServer.SDK.GameEngine.GameEntity { EntityId = 1, X = 10.0f, Y = 20.0f, Z = 30.0f, RotationY = 45.0f });
                interpolator.AddSnapshot(ws1);

                var ws2 = new TeruTeruServer.SDK.GameEngine.WorldState
                {
                    TickNumber = 2,
                    Timestamp = DateTime.UtcNow.AddMilliseconds(100)
                };
                ws2.Entities.TryAdd(1, new TeruTeruServer.SDK.GameEngine.GameEntity { EntityId = 1, X = 20.0f, Y = 40.0f, Z = 60.0f, RotationY = 135.0f });
                interpolator.AddSnapshot(ws2);
                
                // 보간 데이터 가져오기
                var interpolated = interpolator.Interpolate(ws2.Timestamp, 50.0); // 50ms 딜레이 기준
                if (interpolated != null && interpolated.Entities.TryGetValue(1, out var ent))
                {
                    Console.WriteLine($"Interpolated Entity State (50ms delay): X={ent.X:F2}, Y={ent.Y:F2}, Z={ent.Z:F2}, Rotation={ent.RotationY:F2}");
                }

                // 8. ClientPredictionBuffer 데모
                Console.WriteLine("\n[7] Testing ClientPredictionBuffer...");
                var predictionBuffer = new ClientPredictionBuffer();
                
                var input1 = new TeruTeruServer.SDK.GameEngine.GameInput { ClientTick = 1, MoveX = 1.0f };
                var state1 = new TeruTeruServer.SDK.GameEngine.GameEntity { EntityId = 1, X = 1.0f, Y = 0.0f, Z = 1.0f };
                predictionBuffer.RecordInput(input1, state1);

                var input2 = new TeruTeruServer.SDK.GameEngine.GameInput { ClientTick = 2, MoveX = 1.0f };
                var state2 = new TeruTeruServer.SDK.GameEngine.GameEntity { EntityId = 1, X = 2.0f, Y = 0.0f, Z = 2.0f };
                predictionBuffer.RecordInput(input2, state2);

                // Reconcile 시뮬레이션 위임자 정의
                Action<TeruTeruServer.SDK.GameEngine.GameEntity, TeruTeruServer.SDK.GameEngine.GameInput> simDelegate = (entity, input) =>
                {
                    entity.X += 1.0f; // 간단한 이동 시뮬레이션
                    entity.Z += 1.0f;
                };

                // Case A: 작은 오차 (임계값 0.1f 이내)
                var ackSmall = new TeruTeruServer.SDK.GameEngine.StateAck 
                { 
                    LastProcessedClientTick = 1, 
                    X = 1.01f, 
                    Y = 0.0f, 
                    Z = 0.99f 
                };
                var reconciledStateA = predictionBuffer.Reconcile(ackSmall, simDelegate, epsilon: 0.1f);
                Console.WriteLine($"Reconciled state after small error: X={reconciledStateA?.X:F2}, Z={reconciledStateA?.Z:F2}, BufferCount={predictionBuffer.BufferCount}");

                // Case B: 큰 오차 (임계값 0.1f 초과) -> 보정 및 재시뮬레이션 재생 발생
                predictionBuffer.Clear();
                predictionBuffer.RecordInput(input1, state1);
                predictionBuffer.RecordInput(input2, state2);

                var ackLarge = new TeruTeruServer.SDK.GameEngine.StateAck 
                { 
                    LastProcessedClientTick = 1, 
                    X = 1.5f, 
                    Y = 0.0f, 
                    Z = 1.5f 
                };
                var reconciledStateB = predictionBuffer.Reconcile(ackLarge, simDelegate, epsilon: 0.1f);
                Console.WriteLine($"Reconciled state after large error (Replayed): X={reconciledStateB?.X:F2}, Z={reconciledStateB?.Z:F2}, BufferCount={predictionBuffer.BufferCount}");

                // 9. 실시간 대기 및 Reconnect 시뮬레이션
                Console.WriteLine("\n=======================================================");
                Console.WriteLine("Instructions:");
                Console.WriteLine("  Press 'r' to simulate connection drop and reconnect");
                Console.WriteLine("  Press 'q' to exit");
                Console.WriteLine("=======================================================");

                // 세션 토큰 저장
                int savedHostId = client.HostId;
                string? savedToken = client.ReconnectToken;

                bool autoMode = args.Length > 0 && args[0].Equals("auto", StringComparison.OrdinalIgnoreCase);
                if (autoMode)
                {
                    Console.WriteLine("[AutoMode] Running automatic reconnection simulation in 2 seconds...");
                    await Task.Delay(2000);
                    
                    Console.WriteLine("\n[Simulate] Dropping connection...");
                    client.Dispose(); // 기존 클라이언트 소켓 해제
                    Console.WriteLine("Disconnected from server. Waiting 2 seconds before reconnecting...");
                    await Task.Delay(2000);

                    Console.WriteLine("\n[Reconnect] Attempting to reconnect using saved token...");
                    using var reconnectClient = new TeruClient("127.0.0.1", 3000);
                    reconnectClient.OnLog += msg => Console.WriteLine($"[ReconnectLog] {msg}");
                    
                    bool reconnected = await reconnectClient.ReconnectAsync(savedHostId, savedToken!);
                    if (reconnected)
                    {
                        Console.WriteLine("[Reconnect Success] Session successfully restored!");
                    }
                    else
                    {
                        Console.WriteLine("[Reconnect Failed] Could not restore session.");
                    }
                    
                    Console.WriteLine("[AutoMode] Simulation finished. Exiting...");
                }
                else
                {
                    while (true)
                    {
                        var keyChar = Console.ReadKey(true).KeyChar;
                        if (keyChar == 'q' || keyChar == 'Q')
                        {
                            break;
                        }
                        else if (keyChar == 'r' || keyChar == 'R')
                        {
                            Console.WriteLine("\n[Simulate] Dropping connection...");
                            client.Dispose(); // 기존 클라이언트 소켓 해제
                            Console.WriteLine("Disconnected from server. Waiting 2 seconds before reconnecting...");
                            await Task.Delay(2000);

                            Console.WriteLine("\n[Reconnect] Attempting to reconnect using saved token...");
                            using var reconnectClient = new TeruClient("127.0.0.1", 3000);
                            reconnectClient.OnLog += msg => Console.WriteLine($"[ReconnectLog] {msg}");
                            
                            bool reconnected = await reconnectClient.ReconnectAsync(savedHostId, savedToken!);
                            if (reconnected)
                            {
                                Console.WriteLine("[Reconnect Success] Session successfully restored!");
                            }
                            else
                            {
                                Console.WriteLine("[Reconnect Failed] Could not restore session.");
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("[Error] Login failed.");
            }
        }
    }
}
