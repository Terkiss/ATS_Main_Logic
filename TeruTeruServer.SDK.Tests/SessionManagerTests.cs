using System;
using System.Net.Sockets;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.SDK.Enums;
using Xunit;

namespace TeruTeruServer.SDK.Tests
{
    [Collection("ServerMemoryCollection")]
    public class SessionManagerTests
    {
        private ISessionManager CreateManager() => new SessionManager(new TeruTeruServer.SDK.Clustering.InMemorySessionStore());

        [Fact]
        public void AddPlayer_ShouldStoreClientSession()
        {
            var manager = CreateManager();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(10, socket, "game10");

            bool added = manager.TryAddPlayer(10, session);

            Assert.True(added);
            Assert.True(manager.Players.ContainsKey(10));
            Assert.Equal(session, manager.Players[10]);
        }

        [Fact]
        public void MarkAsGrace_ShouldChangeStateToGrace()
        {
            var manager = CreateManager();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(11, socket, "game11");
            manager.TryAddPlayer(11, session);

            bool marked = manager.MarkAsGrace(11);

            Assert.True(marked);
            Assert.Equal(SessionState.Grace, session.State);
            Assert.Null(session.ClientSocket);
        }

        [Fact]
        public void EvictSession_ShouldRemoveSessionCompletely()
        {
            var manager = CreateManager();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(12, socket, "game12");
            manager.TryAddPlayer(12, session);

            bool evicted = manager.EvictSession(12, out var removedSession);

            Assert.True(evicted);
            Assert.Equal(session, removedSession);
            Assert.False(manager.Players.ContainsKey(12));
        }

        [Fact]
        public void TryGetHostIdBySocket_ShouldFindHostId()
        {
            var manager = CreateManager();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var session = new ClientSession(13, socket, "game13");
            manager.TryAddPlayer(13, session);

            bool found = manager.TryGetHostIdBySocket(socket, out int hostId);

            Assert.True(found);
            Assert.Equal(13, hostId);
        }
    }
}
