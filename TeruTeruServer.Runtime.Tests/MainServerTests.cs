using System;
using System.Net.Sockets;
using Moq;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.Runtime;
using TeruTeruServer.Runtime.GameEngine;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class MainServerTests
    {
        [Fact]
        public void MainServer_Initialization_ShouldNotThrow()
        {
            var mockLogic = new Mock<ILogicService>();
            var mockSessionManager = new Mock<ISessionManager>();
            var mockSessionStore = new Mock<ISessionStore>();
            var mockSecurityLogger = new Mock<ISecurityEventLogger>();
            var mockSanctionManager = new SanctionManager(mockSecurityLogger.Object, mockSessionManager.Object);
            var config = new ServerConnectConfigParameter { Port = 12345, MaxConnection = 10, IsUdp = true, IsTcp = false, SendBufferSize = 1024, ReceiveBufferSize = 1024, Guid = "test" };

            var exception = Record.Exception(() => new MainServer(config, mockLogic.Object, mockSessionManager.Object, mockSessionStore.Object, mockSecurityLogger.Object, mockSanctionManager));

            Assert.Null(exception);
        }

        [Fact]
        public void MainServer_SocketCheck_ShouldMarkDisconnectedSocketAsGrace()
        {
            var mockLogic = new Mock<ILogicService>();
            var mockSessionManager = new Mock<ISessionManager>();
            var mockSessionStore = new Mock<ISessionStore>();
            var config = new ServerConnectConfigParameter { Port = 12345, MaxConnection = 10, IsUdp = false, IsTcp = true, SendBufferSize = 1024, ReceiveBufferSize = 1024, Guid = "test" };
            var mockSecurityLogger = new Mock<ISecurityEventLogger>();
            var mockSanctionManager = new SanctionManager(mockSecurityLogger.Object, mockSessionManager.Object);
            var server = new MainServer(config, mockLogic.Object, mockSessionManager.Object, mockSessionStore.Object, mockSecurityLogger.Object, mockSanctionManager);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(1, socket, "test");
            session.State = SessionState.Connected;

            // Fake dictionary
            var dict = new System.Collections.Concurrent.ConcurrentDictionary<int, ClientSession>();
            dict.TryAdd(1, session);
            mockSessionManager.Setup(m => m.Players).Returns(dict);

            // socket is not connected, IsConnected should return false
            server.SocketCheck();

            // Verify MarkAsGrace was called
            mockSessionManager.Verify(m => m.MarkAsGrace(1), Times.Once);
        }

        [Fact]
        public void MainServer_SocketCheck_ShouldEvictGraceAfterTimeout()
        {
            var mockLogic = new Mock<ILogicService>();
            var mockSessionManager = new Mock<ISessionManager>();
            var config = new ServerConnectConfigParameter { Port = 12345, MaxConnection = 10, IsUdp = false, IsTcp = true, SendBufferSize = 1024, ReceiveBufferSize = 1024, Guid = "test" };
            var mockSecurityLogger = new Mock<ISecurityEventLogger>();
            var mockSanctionManager = new SanctionManager(mockSecurityLogger.Object, mockSessionManager.Object);
            var server = new MainServer(config, mockLogic.Object, mockSessionManager.Object, new Mock<ISessionStore>().Object, mockSecurityLogger.Object, mockSanctionManager);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(2, socket, "test");
            session.State = SessionState.Grace;
            session.LastSeenUtc = DateTime.UtcNow.AddSeconds(-35); // over 30s timeout

            var dict = new System.Collections.Concurrent.ConcurrentDictionary<int, ClientSession>();
            dict.TryAdd(2, session);
            mockSessionManager.Setup(m => m.Players).Returns(dict);

            // Need to setup EvictSession to return true so HandleDisconnectedSession proceeds
            ClientSession removedSession;
            mockSessionManager.Setup(m => m.EvictSession(2, out removedSession)).Returns(true);

            server.SocketCheck();

            mockSessionManager.Verify(m => m.EvictSession(2, out It.Ref<ClientSession>.IsAny), Times.Once);
        }
    }
}
