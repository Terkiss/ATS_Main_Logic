using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using TeruTeruServer.Runtime.Clustering;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class ClusteringTests
    {
        [Fact]
        public void ClusterRouter_ShouldSelectLeastLoadedNode()
        {
            var mockRegistry = new Mock<IClusterRegistry>();
            var nodes = new List<ClusterNodeInfo>
            {
                new ClusterNodeInfo { NodeId = "Node1", CurrentConnections = 100, Status = "Active" },
                new ClusterNodeInfo { NodeId = "Node2", CurrentConnections = 50, Status = "Active" },
                new ClusterNodeInfo { NodeId = "Node3", CurrentConnections = 200, Status = "Active" },
                new ClusterNodeInfo { NodeId = "Node4", CurrentConnections = 10, Status = "Draining" }
            };
            mockRegistry.Setup(r => r.GetActiveNodes()).Returns(nodes);

            var router = new ClusterRouter(mockRegistry.Object);
            var selected = router.SelectLeastLoadedNode();

            Assert.Equal("Node2", selected?.NodeId);
        }

        [Fact]
        public void NodeHealthMonitor_ShouldMarkDownOnTimeout()
        {
            var mockRegistry = new Mock<IClusterRegistry>();
            var mockBus = new Mock<IEventBus>();
            var node = new ClusterNodeInfo 
            { 
                NodeId = "Node1", 
                LastHeartbeat = DateTime.UtcNow.AddSeconds(-40), // 30s timeout
                Status = "Active" 
            };
            mockRegistry.Setup(r => r.GetActiveNodes()).Returns(new List<ClusterNodeInfo> { node });

            var monitor = new NodeHealthMonitor(mockRegistry.Object, mockBus.Object);
            monitor.CheckHealth();

            Assert.Equal("Down", node.Status);
            mockBus.Verify(b => b.Publish("cluster:node:down", "Node1"), Times.Once);
        }

        [Fact]
        public void RollingUpdate_ShouldBeReadyWhenSessionsAreZero()
        {
            var mockRegistry = new Mock<IClusterRegistry>();
            var mockSessionManager = new Mock<IGameSessionManager>();
            var node = new ClusterNodeInfo { NodeId = "Node1", Status = "Draining", ActiveSessionCount = 0 };
            mockRegistry.Setup(r => r.GetNode("Node1")).Returns(node);

            var coordinator = new RollingUpdateCoordinator(mockRegistry.Object, mockSessionManager.Object);
            
            Assert.True(coordinator.IsReadyForShutdown("Node1"));
            
            node.ActiveSessionCount = 5;
            Assert.False(coordinator.IsReadyForShutdown("Node1"));
        }

        [Fact]
        public void AutoScale_ShouldDecideScaleUpOnHighLoad()
        {
            var mockRegistry = new Mock<IClusterRegistry>();
            var mockBus = new Mock<IEventBus>();
            var nodes = new List<ClusterNodeInfo>
            {
                new ClusterNodeInfo { NodeId = "N1", CurrentConnections = 850 }, // 85%
                new ClusterNodeInfo { NodeId = "N2", CurrentConnections = 950 }  // 95%
            };
            mockRegistry.Setup(r => r.GetActiveNodes()).Returns(nodes);

            var monitor = new AutoScaleMonitor(mockRegistry.Object, mockBus.Object);
            var decision = monitor.Evaluate();

            Assert.Equal(ScaleDecision.ScaleUp, decision);
        }

        [Fact]
        public void ClusterDashboard_ShouldCalculateSnapshotCorrectly()
        {
            var mockRegistry = new Mock<IClusterRegistry>();
            var nodes = new List<ClusterNodeInfo>
            {
                new ClusterNodeInfo { Status = "Active", CurrentConnections = 10, ActiveZoneCount = 1, ActiveSessionCount = 1 },
                new ClusterNodeInfo { Status = "Active", CurrentConnections = 20, ActiveZoneCount = 2, ActiveSessionCount = 2 },
                new ClusterNodeInfo { Status = "Draining", CurrentConnections = 5, ActiveZoneCount = 1, ActiveSessionCount = 0 }
            };
            mockRegistry.Setup(r => r.GetActiveNodes()).Returns(nodes);

            var dashboard = new ClusterDashboard(
                mockRegistry.Object, 
                new Mock<ISessionManager>().Object,
                new Mock<IZoneManager>().Object,
                new Mock<IGameSessionManager>().Object);

            var snapshot = dashboard.GetSnapshot();

            Assert.Equal(3, snapshot.TotalNodes);
            Assert.Equal(2, snapshot.ActiveNodes);
            Assert.Equal(1, snapshot.DrainingNodes);
            Assert.Equal(35, snapshot.TotalCcu);
            Assert.Equal(4, snapshot.TotalZones);
            Assert.Equal(3, snapshot.TotalSessions);
        }
    }
}
