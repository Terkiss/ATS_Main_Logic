using System;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// Redis에 ClientSession을 안전하게 직렬화하여 저장하기 위한 경량 DTO 클래스입니다.
    /// Socket과 EndPoint처럼 직렬화가 불가능한 시스템 자원 필드들을 제외합니다.
    /// </summary>
    public class RedisSessionData
    {
        public int HostID { get; set; }
        public string GameID { get; set; } = string.Empty;
        public string HostIP { get; set; } = string.Empty;
        public int HostPort { get; set; }
        public string Role { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public DateTime LastSeenUtc { get; set; }
        public SessionState State { get; set; }
        public string ReconnectToken { get; set; } = string.Empty;
        public P2PStatus P2PState { get; set; }
        public int CurrentSecondPacketCount { get; set; }
        public DateTime LastPacketTime { get; set; }
        public uint LastSequenceNumber { get; set; }
        public bool IsAuthenticated { get; set; }
        public long RttMs { get; set; }
        public double PacketLossRate { get; set; }
        public DateTime LastPingUtc { get; set; }
        public int ViolationCount { get; set; }
        public int BanLevel { get; set; }
        public DateTime LastViolationUtc { get; set; }
        public int InputCountThisTick { get; set; }
        public long LastInputTick { get; set; }
        public int Mmr { get; set; }

        // RttTracker 내 샘플 데이터 백업용
        public long AverageRttMs { get; set; }
        public long JitterMs { get; set; }

        public static RedisSessionData FromSession(ClientSession session)
        {
            return new RedisSessionData
            {
                HostID = session.HostID,
                GameID = session.GameID,
                HostIP = session.HostIP,
                HostPort = session.HostPort,
                Role = session.Role,
                ClientName = session.ClientName,
                AuthToken = session.AuthToken,
                LastSeenUtc = session.LastSeenUtc,
                State = session.State,
                ReconnectToken = session.ReconnectToken,
                P2PState = session.P2PState,
                CurrentSecondPacketCount = session.CurrentSecondPacketCount,
                LastPacketTime = session.LastPacketTime,
                LastSequenceNumber = session.LastSequenceNumber,
                IsAuthenticated = session.IsAuthenticated,
                RttMs = session.RttMs,
                PacketLossRate = session.PacketLossRate,
                LastPingUtc = session.LastPingUtc,
                ViolationCount = session.ViolationCount,
                BanLevel = session.BanLevel,
                LastViolationUtc = session.LastViolationUtc,
                InputCountThisTick = session.InputCountThisTick,
                LastInputTick = session.LastInputTick,
                Mmr = session.Mmr,
                AverageRttMs = session.RttHistory?.AverageRttMs ?? 0,
                JitterMs = session.RttHistory?.JitterMs ?? 0
            };
        }

        public ClientSession ToSession()
        {
            var session = new ClientSession(HostID, null, GameID)
            {
                HostIP = HostIP,
                HostPort = HostPort,
                Role = Role,
                ClientName = ClientName,
                AuthToken = AuthToken,
                LastSeenUtc = LastSeenUtc,
                State = State,
                ReconnectToken = ReconnectToken,
                P2PState = P2PState,
                CurrentSecondPacketCount = CurrentSecondPacketCount,
                LastPacketTime = LastPacketTime,
                LastSequenceNumber = LastSequenceNumber,
                IsAuthenticated = IsAuthenticated,
                RttMs = RttMs,
                PacketLossRate = PacketLossRate,
                LastPingUtc = LastPingUtc,
                ViolationCount = ViolationCount,
                BanLevel = BanLevel,
                LastViolationUtc = LastViolationUtc,
                InputCountThisTick = InputCountThisTick,
                LastInputTick = LastInputTick,
                Mmr = Mmr
            };

            if (session.RttHistory != null && AverageRttMs > 0)
            {
                session.UpdateRtt(AverageRttMs);
            }

            return session;
        }
    }
}
