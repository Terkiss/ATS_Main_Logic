using System;
using System.Net.Sockets;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.SDK.Enums;
using Xunit;

namespace TeruTeruServer.SDK.Tests
{
    public class ClientSessionTests
    {
        [Fact]
        public void ClientSession_ShouldInitialize_Correctly()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(1, socket, "gameID_1");

            Assert.Equal(socket, session.ClientSocket);
            Assert.Equal(SessionState.Connected, session.State);
            Assert.Equal(P2PStatus.Signaling, session.P2PState);
            Assert.False(string.IsNullOrEmpty(session.ReconnectToken));
            Assert.True((DateTime.UtcNow - session.LastSeenUtc).TotalSeconds < 1);
        }

        [Fact]
        public void ClientSession_UpdateLastSeen_ShouldWork()
        {
            var session = new ClientSession(2, null, "gameID_2");
            var oldTime = session.LastSeenUtc;

            System.Threading.Thread.Sleep(10);
            session.UpdateLastSeen();

            Assert.True(session.LastSeenUtc > oldTime);
        }
    }
}
