using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TeruTeruServer.Logic.Default;
using TeruTeruServer.Runtime.Testing;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Models;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Util;
using Xunit;

namespace TeruTeruServer.Runtime.Tests.Integration
{
    public class LoginFlowTests
    {
        private readonly IServiceProvider _serviceProvider;

        public LoginFlowTests()
        {
            var services = new ServiceCollection();

            // Mock dependencies
            var mockMsgSender = new MockMessageSender();
            services.AddSingleton<IMessageSender>(mockMsgSender);
            services.AddSingleton<IDatabaseService>(new Mock<IDatabaseService>().Object);
            services.AddSingleton<ISessionStore, InMemorySessionStore>();
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IEventBus, LocalEventBus>();
            services.AddSingleton<IProtocolRouter, Rpc.ProtocolRouter>();

            // Logic Service (LogicPlugin)
            services.AddSingleton<ILogicService>(sp => 
            {
                var sender = sp.GetRequiredService<IMessageSender>();
                var db = sp.GetRequiredService<IDatabaseService>();
                var session = sp.GetRequiredService<ISessionManager>();
                var router = sp.GetRequiredService<IProtocolRouter>();
                var bus = sp.GetRequiredService<IEventBus>();
                return new LogicPlugin(sender, db, session, router, bus);
            });

            _serviceProvider = services.BuildServiceProvider();
            
            // 필수: 라우터 초기화 (LogicPlugin 생성자에서 호출됨)
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccessWithJwt()
        {
            // Arrange
            using var mockServer = new MockServer(_serviceProvider);
            var simulator = new PacketSimulator(mockServer);
            var loginReq = new LoginProtocol { UserId = "testuser", Password = "testpassword" };

            // Act
            var response = await simulator.SendAndReceive<LoginProtocol>(ProtocolSelect.LoginProtocol, loginReq);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.False(string.IsNullOrEmpty(response.AuthToken));
            Assert.False(string.IsNullOrEmpty(response.RefreshToken));
        }

        [Fact]
        public async Task Connection_Handshake_ReturnsSuccess()
        {
            // Arrange
            using var mockServer = new MockServer(_serviceProvider);
            var simulator = new PacketSimulator(mockServer);
            var connectReq = new ConnectProtocol { Guid = Guid.NewGuid().ToString() };

            // Act
            var response = await simulator.SendAndReceive<ConnectProtocol>(ProtocolSelect.ConnectProtocol, connectReq);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
        }
    }
}
