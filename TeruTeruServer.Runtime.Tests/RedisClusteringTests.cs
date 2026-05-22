using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using TeruTeruServer.Runtime.Clustering;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class RedisClusteringTests
    {
        private const string InvalidConnectionString = "127.0.0.1:9999,connectTimeout=1000,syncTimeout=1000";

        [Fact]
        public void RedisSessionStore_ShouldFallbackToLocalCache_OnInvalidConnection()
        {
            // Arrange
            var store = new RedisSessionStore(InvalidConnectionString);
            
            // ClientSession 생성 시 null socket 전달
            var session = new ClientSession(101, null, "Game_101")
            {
                HostIP = "127.0.0.1",
                HostPort = 12345,
                Role = "Player",
                ClientName = "TestPlayer",
                State = SessionState.Connected
            };

            // Act & Assert
            // 1. TryAdd 검증 (Redis 예외가 로깅되나, 메서드는 정상 true 리턴)
            var addResult = store.TryAdd(101, session);
            Assert.True(addResult);

            // 2. TryGet 검증 (로컬 캐시 확인)
            var getResult = store.TryGet(101, out var retrieved);
            Assert.True(getResult);
            Assert.NotNull(retrieved);
            Assert.Equal("Game_101", retrieved.GameID);
            Assert.Null(retrieved.ClientSocket);

            // 3. FindByReconnectToken 검증 (로컬 백업 탐색)
            var token = session.ReconnectToken;
            var found = store.FindByReconnectToken(token);
            Assert.NotNull(found);
            Assert.Equal(101, found.HostID);

            // 4. GetAll 검증
            var all = store.GetAll().ToList();
            Assert.Single(all);
            Assert.Equal(101, all[0].HostID);

            // 5. TryRemove 검증
            var removeResult = store.TryRemove(101, out var removed);
            Assert.True(removeResult);
            Assert.NotNull(removed);

            var getResultAfterRemove = store.TryGet(101, out _);
            Assert.False(getResultAfterRemove);
        }

        [Fact]
        public void RedisEventBus_ShouldFallbackToLocalHandler_OnInvalidConnection()
        {
            // Arrange
            var bus = new RedisEventBus(InvalidConnectionString);
            string receivedMessage = null;
            var signal = new ManualResetEvent(false);

            bus.Subscribe<string>("test:channel", (msg) =>
            {
                receivedMessage = msg;
                signal.Set();
            });

            // Act
            // Redis 연결이 안 되므로 Publish 시 로컬 Failover로 즉시 로컬 핸들러를 실행함
            bus.Publish("test:channel", "Hello World Failover");

            // Assert
            bool signaled = signal.WaitOne(1000);
            Assert.True(signaled);
            Assert.Equal("Hello World Failover", receivedMessage);

            // Clean
            bus.Unsubscribe("test:channel");
        }

        [Fact]
        public void RedisClusterRegistry_ShouldFallbackToLocalCache_OnInvalidConnection()
        {
            // Arrange
            var registry = new RedisClusterRegistry(InvalidConnectionString);
            var node = new ClusterNodeInfo
            {
                NodeId = "ServerNode_01",
                Address = "127.0.0.1",
                Port = 8000,
                Status = "Active",
                CurrentConnections = 10,
                LastHeartbeat = DateTime.UtcNow
            };

            // Act & Assert
            // 1. 등록 검증
            registry.RegisterNode(node);

            // 2. 조회 검증
            var retrieved = registry.GetNode("ServerNode_01");
            Assert.NotNull(retrieved);
            Assert.Equal("127.0.0.1", retrieved.Address);

            // 3. 하트비트 갱신 검증
            var oldHeartbeat = retrieved.LastHeartbeat;
            Thread.Sleep(10);
            registry.UpdateHeartbeat("ServerNode_01");
            var updated = registry.GetNode("ServerNode_01");
            Assert.NotNull(updated);
            Assert.True(updated.LastHeartbeat > oldHeartbeat);

            // 4. 전체 조회 검증
            var activeNodes = registry.GetActiveNodes();
            Assert.Single(activeNodes);
            Assert.Equal("ServerNode_01", activeNodes[0].NodeId);

            // 5. 제거 검증
            registry.DeregisterNode("ServerNode_01");
            var afterDelete = registry.GetNode("ServerNode_01");
            Assert.Null(afterDelete);
        }
    }
}
