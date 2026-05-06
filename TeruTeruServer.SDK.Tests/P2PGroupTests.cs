using System;
using TeruTeruServer.SDK.Models;
using TeruTeruServer.SDK.Util;
using Xunit;
using System.Reflection;

namespace TeruTeruServer.SDK.Tests
{
    public class P2PGroupTests
    {
        public P2PGroupTests()
        {
            // Reset ServerMemory ID to ensure predictable tests
            var field = typeof(ServerMemory).GetField("_currentHostID", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, 1);
        }

        [Fact]
        public void P2PGroup_ShouldAssignCorrectGroupId()
        {
            var group = new P2PGroup(100);

            // GroupId should be 1 since we reset the ID generator
            Assert.Equal(1, group.GroupId);
            Assert.Equal(100, group.OwnerId);
        }

        [Fact]
        public void P2PGroup_AddAndRemoveMember_ShouldWork()
        {
            var group = new P2PGroup(200);

            group.AddMember(201);
            group.AddMember(202);

            var members = group.GetMemberIds();
            Assert.Contains(200, members); // owner is implicitly added
            Assert.Contains(201, members);
            Assert.Contains(202, members);
            Assert.Equal(3, members.Length);

            group.RemoveMember(201);
            members = group.GetMemberIds();
            Assert.DoesNotContain(201, members);
            Assert.Equal(2, members.Length);
        }
    }
}
