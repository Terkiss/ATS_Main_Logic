using Xunit;
using TeruTeruServer.SDK.Util;
using System.Reflection;

namespace TeruTeruServer.SDK.Tests
{
    [Collection("ServerMemoryCollection")]
    public class ServerMemoryTests : IDisposable
    {
        public ServerMemoryTests()
        {
            ResetStaticState();
        }

        public void Dispose()
        {
            ResetStaticState();
        }

        private void ResetStaticState()
        {
            var idField = typeof(ServerMemory).GetField("_currentHostID", BindingFlags.Static | BindingFlags.NonPublic);
            idField?.SetValue(null, 1);

            var hostsField = typeof(ServerMemory).GetField("_hosts", BindingFlags.Static | BindingFlags.NonPublic);
            var hosts = hostsField?.GetValue(null) as System.Collections.IDictionary;
            hosts?.Clear();

            var gameIdField = typeof(ServerMemory).GetField("_gameID2HostID", BindingFlags.Static | BindingFlags.NonPublic);
            var gameIds = gameIdField?.GetValue(null) as System.Collections.IDictionary;
            gameIds?.Clear();

            var q1Field = typeof(ServerMemory).GetField("_imageWorkPreOrderQueue", BindingFlags.Static | BindingFlags.NonPublic);
            if (q1Field?.GetValue(null) is System.Collections.Concurrent.ConcurrentQueue<TeruTeruServer.SDK.Protocol.SendImageData> q1)
                q1.Clear();

            var q2Field = typeof(ServerMemory).GetField("_imageWorkCompleteQueue", BindingFlags.Static | BindingFlags.NonPublic);
            if (q2Field?.GetValue(null) is System.Collections.Concurrent.ConcurrentQueue<TeruTeruServer.SDK.Protocol.YoloDetectResult> q2)
                q2.Clear();
        }

        [Fact]
        public void ServerHostId_ShouldBe_Zero()
        {
            Assert.Equal(0, ServerMemory.SERVER_HOST_ID);
        }

        [Fact]
        public void First_GetHostID_ShouldBe_One()
        {

            int firstId = ServerMemory.GetHostID;
            Assert.Equal(1, firstId);
        }

        [Fact]
        public void GetHostID_Should_Increment()
        {

            int firstId = ServerMemory.GetHostID;
            int secondId = ServerMemory.GetHostID;

            Assert.Equal(1, firstId);
            Assert.Equal(2, secondId);
        }
    }
}
