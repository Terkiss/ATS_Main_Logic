using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using TeruTeruServer.Runtime.Pipeline;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;
using Microsoft.Extensions.DependencyInjection;

namespace TeruTeruServer.Runtime.Testing
{
    /// <summary>
    /// 소켓 없이 패킷 처리를 테스트하기 위한 Mock 패킷 컨텍스트입니다.
    /// </summary>
    public class MockPacketContext : PacketContext
    {
        public List<byte[]> CapturedResponses { get; } = new List<byte[]>();

        public MockPacketContext(byte[] data) : base(null!, data)
        {
        }

        /// <summary>
        /// 응답 전송 시 소켓 대신 리스트에 캡처합니다.
        /// </summary>
        public void SendResponse(byte[] data)
        {
            CapturedResponses.Add(data);
        }
    }

    /// <summary>
    /// 실제 서버의 파이프라인을 그대로 실행하되, 소켓 통신을 바이패스하는 테스트용 서버입니다.
    /// </summary>
    public class MockServer : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly PacketPipeline _pipeline;
        private readonly MockMessageSender _mockSender;

        public MockServer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _mockSender = (MockMessageSender)_serviceProvider.GetRequiredService<IMessageSender>();
            _pipeline = new PacketPipeline();

            // MainServer.InitializePipeline()의 순서를 정확히 복제
            _pipeline.Use(new ValidationMiddleware());
            _pipeline.Use(new RateLimitMiddleware(5000)); // 테스트 중 차단 방지를 위해 높게 설정
            _pipeline.Use(new ReplayAttackMiddleware());
            _pipeline.Use(new DecryptionMiddleware(new SeedCryptoService()));
            
            var sessionManager = _serviceProvider.GetRequiredService<ISessionManager>();
            var sessionStore = _serviceProvider.GetRequiredService<ISessionStore>();
            _pipeline.Use(new AuthMiddleware(sessionManager, sessionStore));
            
            var logicService = _serviceProvider.GetRequiredService<ILogicService>();
            _pipeline.Use(new RoutingMiddleware(logicService));
        }

        /// <summary>
        /// 원본 바이트 패킷을 처리하고 발생한 모든 응답 패킷을 반환합니다.
        /// </summary>
        public async Task<List<byte[]>> ProcessPacketAsync(byte[] rawPacket)
        {
            _mockSender.Clear();
            var context = new PacketContext(null!, rawPacket);
            await _pipeline.ExecuteAsync(context);
            return _mockSender.LastCapturedData;
        }

        /// <summary>
        /// JSON 페이로드를 직접 처리하고 응답을 반환합니다.
        /// </summary>
        public async Task<List<byte[]>> ProcessJsonAsync(ProtocolSelect protocol, object payload)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            
            // 헤더 구성 (6바이트): [Type][Proto][Seq(4)]
            byte[] packet = new byte[body.Length + 6];
            packet[0] = (byte)SendType.Json;
            packet[1] = (byte)protocol;
            // Seq는 0으로 채움 (Mock)
            Buffer.BlockCopy(body, 0, packet, 6, body.Length);

            return await ProcessPacketAsync(packet);
        }

        public void Dispose()
        {
            // 필요한 경우 정리 로직 추가
        }
    }

    /// <summary>
    /// MockServer에서 발생하는 응답을 가로채기 위한 Mock MessageSender입니다.
    /// </summary>
    public class MockMessageSender : IMessageSender
    {
        public List<byte[]> LastCapturedData { get; } = new List<byte[]>();

        public void SendData(Socket socket, byte[] data)
        {
            LastCapturedData.Add(data);
        }

        public void SendData(int hostID, byte[] data)
        {
            LastCapturedData.Add(data);
        }

        public void Clear() => LastCapturedData.Clear();
    }
}
