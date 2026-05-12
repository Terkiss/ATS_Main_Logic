using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.Runtime.GameEngine
{
    public class TeamBalancer
    {
        /// <summary>
        /// MMR 기반으로 팀을 균등 배정합니다. 스네이크 드래프트 방식.
        /// </summary>
        public Dictionary<int, int> AssignTeams(List<MatchEntry> players, int teamCount)
        {
            var assignments = new Dictionary<int, int>();
            if (players == null || players.Count == 0) return assignments;

            // 1. MMR 내림차순 정렬
            var sortedPlayers = players.OrderByDescending(p => p.Mmr).ToList();

            // 2. 스네이크 드래프트: 1→2→...→N→N→...→2→1 순서로 배정
            bool forward = true;
            int currentTeam = 0;

            foreach (var player in sortedPlayers)
            {
                assignments[player.HostId] = currentTeam;

                if (forward)
                {
                    if (currentTeam == teamCount - 1)
                    {
                        forward = false;
                        // N번째 팀에 한 번 더 배정 (N, N, N-1, ...)
                    }
                    else
                    {
                        currentTeam++;
                    }
                }
                else
                {
                    if (currentTeam == 0)
                    {
                        forward = true;
                        // 0번째 팀에 한 번 더 배정 (0, 0, 1, ...)
                    }
                    else
                    {
                        currentTeam--;
                    }
                }
            }

            return assignments;
        }
    }
}
