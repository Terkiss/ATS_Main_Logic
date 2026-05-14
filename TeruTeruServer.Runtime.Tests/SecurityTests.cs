using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Moq;
using TeruTeruServer.Runtime.GameEngine;
using TeruTeruServer.Runtime.Pipeline;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class SecurityTests
    {
        [Fact]
        public void SanctionManager_ShouldIncreaseViolationCountAndSetBanLevel()
        {
            var mockLogger = new Mock<ISecurityEventLogger>();
            var mockSessionManager = new Mock<ISessionManager>();
            var sanctionManager = new SanctionManager(mockLogger.Object, mockSessionManager.Object);
            
            var session = new ClientSession(1, null!, "test_user");
            var evt = new SecurityEvent { HostId = 1, EventType = "TestViolation" };
            
            // 3회 위반 시 경고 (BanLevel 1)
            for (int i = 0; i < 3; i++)
                sanctionManager.ProcessViolation(session, evt);
                
            Assert.Equal(3, session.ViolationCount);
            Assert.Equal(1, session.BanLevel);
            
            // 7회 위반 시 임시 차단 (BanLevel 2)
            for (int i = 0; i < 4; i++)
                sanctionManager.ProcessViolation(session, evt);
                
            Assert.Equal(7, session.ViolationCount);
            Assert.Equal(2, session.BanLevel);
            
            // 15회 위반 시 영구 차단 (BanLevel 3)
            for (int i = 0; i < 8; i++)
                sanctionManager.ProcessViolation(session, evt);
                
            Assert.Equal(15, session.ViolationCount);
            Assert.Equal(3, session.BanLevel);
            
            mockLogger.Verify(l => l.LogEvent(It.IsAny<SecurityEvent>()), Times.Exactly(15));
        }

        [Fact]
        public void InputFrequencyValidator_ShouldDetectFlood()
        {
            var validator = new InputFrequencyValidator(maxInputsPerTick: 2);
            var session = new ClientSession(1, null!, "test_user");
            long tick = 100;
            
            // 1st input
            var result1 = validator.Validate(session, tick);
            Assert.Null(result1);
            Assert.Equal(1, session.InputCountThisTick);
            
            // 2nd input
            var result2 = validator.Validate(session, tick);
            Assert.Null(result2);
            Assert.Equal(2, session.InputCountThisTick);
            
            // 3rd input (Flood!)
            var result3 = validator.Validate(session, tick);
            Assert.NotNull(result3);
            Assert.Equal("InputFlood", result3!.EventType);
            Assert.Equal(3, session.InputCountThisTick);
            
            // New tick should reset counter
            var result4 = validator.Validate(session, tick + 1);
            Assert.Null(result4);
            Assert.Equal(1, session.InputCountThisTick);
        }

        [Fact]
        public async Task BanCheckMiddleware_ShouldDropPacketIfBanned()
        {
            var middleware = new BanCheckMiddleware();
            var context = new PacketContext(null!, new byte[] { 0 });
            var session = new ClientSession(1, null!, "test_user");
            
            // Case 1: Normal session
            session.BanLevel = 0;
            context.Session = session;
            bool nextCalled = false;
            await middleware.InvokeAsync(context, () => { nextCalled = true; return Task.CompletedTask; });
            Assert.True(nextCalled);
            Assert.False(context.IsProcessed);
            
            // Case 2: Banned session (Level 2)
            session.BanLevel = 2;
            nextCalled = false;
            await middleware.InvokeAsync(context, () => { nextCalled = true; return Task.CompletedTask; });
            Assert.False(nextCalled);
            Assert.True(context.IsProcessed);
        }

        [Fact]
        public async Task HmacVerifyMiddleware_ShouldVerifyCorrectHmac()
        {
            byte[] key = Encoding.UTF8.GetBytes("test_key");
            var mockLogger = new Mock<ISecurityEventLogger>();
            var mockSessionManager = new Mock<ISessionManager>();
            var sanctionManager = new SanctionManager(mockLogger.Object, mockSessionManager.Object);
            var middleware = new HmacVerifyMiddleware(key, sanctionManager);
            
            var session = new ClientSession(1, null!, "test_user");
            session.IsAuthenticated = true;
            
            // [SendType(1)][Protocol(1)][Seq(4)][HMAC(32)][Payload(2)]
            byte[] payload = new byte[] { 1, 2 };
            byte[] header = new byte[] { (byte)SendType.Json, (byte)ProtocolSelect.GameInputProtocol, 0, 0, 0, 1 };
            
            byte[] dataToSign = new byte[header.Length + payload.Length];
            Array.Copy(header, 0, dataToSign, 0, header.Length);
            Array.Copy(payload, 0, dataToSign, header.Length, payload.Length);
            
            byte[] hmac;
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(key))
                hmac = hmacsha256.ComputeHash(dataToSign);
                
            byte[] packet = new byte[header.Length + hmac.Length + payload.Length];
            Array.Copy(header, 0, packet, 0, header.Length);
            Array.Copy(hmac, 0, packet, header.Length, hmac.Length);
            Array.Copy(payload, 0, packet, header.Length + hmac.Length, payload.Length);
            
            var context = new PacketContext(null!, packet);
            context.Session = session;
            
            bool nextCalled = false;
            await middleware.InvokeAsync(context, () => { nextCalled = true; return Task.CompletedTask; });
            
            Assert.True(nextCalled);
            Assert.False(context.IsProcessed);
            // Payload should be cleaned (HMAC removed)
            Assert.Equal(header.Length + payload.Length, context.RawData.Length);
        }

        [Fact]
        public async Task HmacVerifyMiddleware_ShouldFailOnTamperedPacket()
        {
            byte[] key = Encoding.UTF8.GetBytes("test_key");
            var mockLogger = new Mock<ISecurityEventLogger>();
            var mockSessionManager = new Mock<ISessionManager>();
            var sanctionManager = new SanctionManager(mockLogger.Object, mockSessionManager.Object);
            var middleware = new HmacVerifyMiddleware(key, sanctionManager);
            
            var session = new ClientSession(1, null!, "test_user");
            session.IsAuthenticated = true;
            
            // Invalid HMAC (all zeros)
            byte[] packet = new byte[40]; 
            var context = new PacketContext(null!, packet);
            context.Session = session;
            
            bool nextCalled = false;
            await middleware.InvokeAsync(context, () => { nextCalled = true; return Task.CompletedTask; });
            
            Assert.False(nextCalled);
            Assert.True(context.IsProcessed);
            Assert.Equal(1, session.ViolationCount);
            mockLogger.Verify(l => l.LogEvent(It.IsAny<SecurityEvent>()), Times.Once);
        }

        [Fact]
        public async Task HmacVerifyMiddleware_ShouldBypassIfUnauthenticated()
        {
            byte[] key = Encoding.UTF8.GetBytes("test_key");
            var middleware = new HmacVerifyMiddleware(key, null!);
            
            var session = new ClientSession(1, null!, "test_user");
            session.IsAuthenticated = false; // Not authenticated
            
            var context = new PacketContext(null!, new byte[10]);
            context.Session = session;
            
            bool nextCalled = false;
            await middleware.InvokeAsync(context, () => { nextCalled = true; return Task.CompletedTask; });
            
            Assert.True(nextCalled);
            Assert.False(context.IsProcessed);
        }
    }
}
