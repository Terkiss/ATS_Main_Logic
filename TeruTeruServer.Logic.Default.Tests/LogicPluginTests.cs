using System.Net.Sockets;
using System.Text.Json;
using Moq;
using TeruTeruServer.Logic.Default;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Protocol;
using Xunit;

namespace TeruTeruServer.Logic.Default.Tests
{
    public class LogicPluginTests
    {
        private readonly Mock<IMessageSender> _mockMessageSender;
        private readonly Mock<IDatabaseService> _mockDbService;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IProtocolRouter> _mockProtocolRouter;
        private readonly Mock<IEventBus> _mockEventBus;
        private readonly LogicPlugin _logicPlugin;

        public LogicPluginTests()
        {
            _mockMessageSender = new Mock<IMessageSender>();
            _mockDbService = new Mock<IDatabaseService>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockProtocolRouter = new Mock<IProtocolRouter>();
            _mockEventBus = new Mock<IEventBus>();

            _logicPlugin = new LogicPlugin(
                _mockMessageSender.Object,
                _mockDbService.Object,
                _mockSessionManager.Object,
                _mockProtocolRouter.Object,
                _mockEventBus.Object);
        }

        [Fact]
        public void HandleLogin_ValidCredentials_SendsSuccessResponse()
        {
            // Arrange
            var loginProtocol = new LoginProtocol
            {
                UserId = "testuser",
                Password = "password123"
            };
            string json = JsonSerializer.Serialize(loginProtocol);
            var mockSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Act
            _logicPlugin.HandleLogin(mockSocket, loginProtocol);

            // Assert
            _mockMessageSender.Verify(x => x.SendData(
                It.IsAny<Socket>(),
                It.Is<byte[]>(data => data[0] == (byte)SendType.Json && data[1] == (byte)ProtocolSelect.LoginProtocol)),
                Times.Once);
        }

        [Fact]
        public void ConProtocol_ValidRequest_SendsSuccessResponse()
        {
            // Arrange
            var connectProtocol = new ConnectProtocol
            {
                Guid = Guid.NewGuid().ToString()
            };
            string json = JsonSerializer.Serialize(connectProtocol);
            var mockSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Act
            _logicPlugin.ConProtocol(mockSocket, connectProtocol);

            // Assert
            _mockMessageSender.Verify(x => x.SendData(
                It.IsAny<Socket>(),
                It.Is<byte[]>(data => data[0] == (byte)SendType.Json && data[1] == (byte)ProtocolSelect.ConnectProtocol)),
                Times.Once);
        }


    }
}
