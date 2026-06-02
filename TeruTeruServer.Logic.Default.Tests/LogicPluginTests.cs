using System;
using System.Net.Sockets;
using System.Text.Json;
using Moq;
using TeruTeruServer.Logic.Default;
using TeruTeruServer.Logic.Default.Services;
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
        private readonly Mock<IZoneManager> _mockZoneManager;
        private readonly LogicPlugin _logicPlugin;

        public LogicPluginTests()
        {
            _mockMessageSender = new Mock<IMessageSender>();
            _mockDbService = new Mock<IDatabaseService>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockProtocolRouter = new Mock<IProtocolRouter>();
            _mockEventBus = new Mock<IEventBus>();
            _mockZoneManager = new Mock<IZoneManager>();

            _logicPlugin = new LogicPlugin(
                _mockMessageSender.Object,
                _mockDbService.Object,
                _mockSessionManager.Object,
                _mockProtocolRouter.Object,
                _mockEventBus.Object,
                _mockZoneManager.Object);
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

        [Fact]
        public void TokenProvider_GenerateAndValidate_Success()
        {
            // Arrange
            var provider = new JwtTokenProvider();
            string userId = "testUser123";

            // Act
            string accessToken = provider.GenerateJwtToken(userId, out string refreshToken);

            // Assert
            Assert.False(string.IsNullOrEmpty(accessToken));
            Assert.False(string.IsNullOrEmpty(refreshToken));

            bool isValid = provider.ValidateRefreshToken(refreshToken, out string extractedUserId);
            Assert.True(isValid);
            Assert.Equal(userId, extractedUserId);
        }

        [Fact]
        public void TokenProvider_ValidateInvalidToken_ReturnsFalse()
        {
            // Arrange
            var provider = new JwtTokenProvider();
            string invalidToken = "invalid.token.value";

            // Act
            bool isValid = provider.ValidateRefreshToken(invalidToken, out string userId);

            // Assert
            Assert.False(isValid);
            Assert.True(string.IsNullOrEmpty(userId));
        }

        [Fact]
        public void HandleTokenRefresh_ValidToken_GeneratesNewTokens()
        {
            // Arrange
            var mockTokenProvider = new Mock<ITokenProvider>();
            string oldRefreshToken = "old_refresh_token";
            string newAccessToken = "new_access_token";
            string newRefreshToken = "new_refresh_token";
            string userId = "testUser";

            mockTokenProvider.Setup(p => p.ValidateRefreshToken(oldRefreshToken, out userId))
                .Returns(true);
            mockTokenProvider.Setup(p => p.GenerateJwtToken(userId, out newRefreshToken))
                .Returns(newAccessToken);

            var localLogicPlugin = new LogicPlugin(
                _mockMessageSender.Object,
                _mockDbService.Object,
                _mockSessionManager.Object,
                _mockProtocolRouter.Object,
                _mockEventBus.Object,
                _mockZoneManager.Object,
                mockTokenProvider.Object);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var refreshProtocol = new TokenRefreshProtocol
            {
                RefreshToken = oldRefreshToken
            };

            // Act
            localLogicPlugin.HandleTokenRefresh(socket, refreshProtocol);

            // Assert
            Assert.True(refreshProtocol.IsSuccess);
            Assert.Equal(newAccessToken, refreshProtocol.NewAuthToken);
            Assert.Equal(newRefreshToken, refreshProtocol.NewRefreshToken);

            _mockMessageSender.Verify(x => x.SendData(
                It.IsAny<Socket>(),
                It.Is<byte[]>(data => data[0] == (byte)SendType.Json && data[1] == (byte)ProtocolSelect.TokenRefreshProtocol)),
                Times.Once);
        }
    }
}
