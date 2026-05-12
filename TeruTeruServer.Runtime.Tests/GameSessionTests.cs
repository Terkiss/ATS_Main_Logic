using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TeruTeruServer.Runtime.GameEngine;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class GameSessionTests
    {
        [Fact]
        public void TeamBalancer_ShouldAssignTeamsEquallyUsingSnakeDraft()
        {
            var balancer = new TeamBalancer();
            var players = new List<MatchEntry>
            {
                new MatchEntry { HostId = 1, Mmr = 2000 },
                new MatchEntry { HostId = 2, Mmr = 1800 },
                new MatchEntry { HostId = 3, Mmr = 1600 },
                new MatchEntry { HostId = 4, Mmr = 1400 },
                new MatchEntry { HostId = 5, Mmr = 1200 },
                new MatchEntry { HostId = 6, Mmr = 1000 }
            };

            var assignments = balancer.AssignTeams(players, 2);

            // Snake Draft: 1(T0), 2(T1), 3(T1), 4(T0), 5(T0), 6(T1)
            Assert.Equal(0, assignments[1]);
            Assert.Equal(1, assignments[2]);
            Assert.Equal(1, assignments[3]);
            Assert.Equal(0, assignments[4]);
            Assert.Equal(0, assignments[5]);
            Assert.Equal(1, assignments[6]);

            var team0Mmr = players.Where(p => assignments[p.HostId] == 0).Average(p => p.Mmr);
            var team1Mmr = players.Where(p => assignments[p.HostId] == 1).Average(p => p.Mmr);

            // (2000 + 1400 + 1200) / 3 = 1533.3
            // (1800 + 1600 + 1000) / 3 = 1466.6
            Assert.True(Math.Abs(team0Mmr - team1Mmr) < 100);
        }

        [Fact]
        public void GameSessionManager_ShouldTransitionStatesCorrectly()
        {
            var mockBus = new Mock<IEventBus>();
            var mockBroadcaster = new Mock<IRoomBroadcaster>();
            var manager = new GameSessionManager(mockBus.Object, mockBroadcaster.Object);

            var players = new List<MatchEntry> { new MatchEntry { HostId = 1, Mmr = 1000 } };
            var session = manager.CreateSession(players, 1);

            Assert.Equal(GameSessionState.MatchFound, session.State);

            // Success transitions
            Assert.True(manager.TransitionState(session.SessionId, GameSessionState.Loading));
            Assert.True(manager.TransitionState(session.SessionId, GameSessionState.InGame));
            
            // Failure: Backward transition
            Assert.False(manager.TransitionState(session.SessionId, GameSessionState.MatchFound));
            Assert.Equal(GameSessionState.InGame, session.State);

            // InGame -> Result publishes event
            Assert.True(manager.TransitionState(session.SessionId, GameSessionState.Result));
            mockBus.Verify(b => b.Publish(It.IsAny<string>(), It.IsAny<GameResult>()), Times.Once);
        }

        [Fact]
        public void MatchQueue_ShouldMatchWithinMmrRange()
        {
            var mockManager = new Mock<IGameSessionManager>();
            var queue = new MatchQueue(mockManager.Object, teamSize: 1, teamCount: 2, mmrRange: 200);

            queue.Enqueue(new MatchEntry { HostId = 1, Mmr = 1000 });
            queue.Enqueue(new MatchEntry { HostId = 2, Mmr = 1500 }); // Out of range
            queue.Enqueue(new MatchEntry { HostId = 3, Mmr = 1100 }); // Within range with Host 1

            queue.TryMatch();

            // Should match 1 and 3
            mockManager.Verify(m => m.CreateSession(It.Is<List<MatchEntry>>(l => l.Any(p => p.HostId == 1) && l.Any(p => p.HostId == 3)), 2), Times.Once);
            Assert.Equal(1, queue.QueueLength); // Host 2 still in queue
        }

        [Fact]
        public void GameSessionManager_Spectator_ShouldOnlyBeAddedInGame()
        {
            var mockBus = new Mock<IEventBus>();
            var mockBroadcaster = new Mock<IRoomBroadcaster>();
            var manager = new GameSessionManager(mockBus.Object, mockBroadcaster.Object);

            var session = manager.CreateSession(new List<MatchEntry> { new MatchEntry { HostId = 1 } }, 1);

            // Lobby/MatchFound state
            Assert.False(manager.AddSpectator(session.SessionId, 100));

            manager.TransitionState(session.SessionId, GameSessionState.Loading);
            manager.TransitionState(session.SessionId, GameSessionState.InGame);

            // InGame state
            Assert.True(manager.AddSpectator(session.SessionId, 100));
            Assert.Contains(100, session.SpectatorHostIds);
            Assert.DoesNotContain(100, session.PlayerHostIds);
        }
    }
}
