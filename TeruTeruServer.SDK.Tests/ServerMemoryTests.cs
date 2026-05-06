using Xunit;
using TeruTeruServer.SDK.Util;
using System.Reflection;

namespace TeruTeruServer.SDK.Tests
{
    public class ServerMemoryTests
    {
        public ServerMemoryTests()
        {
            // Reset the static state before each test
            var field = typeof(ServerMemory).GetField("_currentHostID", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, 1);
        }

        [Fact]
        public void ServerHostId_ShouldBe_Zero()
        {
            Assert.Equal(0, ServerMemory.SERVER_HOST_ID);
        }

        [Fact]
        public void First_GetHostID_ShouldBe_One()
        {
            var field = typeof(ServerMemory).GetField("_currentHostID", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, 1); // Ensure starting at 1
            
            int firstId = ServerMemory.GetHostID;
            Assert.Equal(1, firstId);
        }

        [Fact]
        public void GetHostID_Should_Increment()
        {
            var field = typeof(ServerMemory).GetField("_currentHostID", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, 1);

            int firstId = ServerMemory.GetHostID;
            int secondId = ServerMemory.GetHostID;

            Assert.Equal(1, firstId);
            Assert.Equal(2, secondId);
        }
    }
}
