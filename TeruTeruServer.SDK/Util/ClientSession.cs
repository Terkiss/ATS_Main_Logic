using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.SDK.Util
{
    /// <summary>
    /// 클라이언트 세션 정보를 관리하는 클래스입니다.
    /// </summary>
    public class ClientSession
    {
        public int HostID { get; set; } // 호스트 고유 ID
        public string GameID { get; set; } // 게임 내 고유 ID
        public string HostIP { get; set; } // 호스트 IP 주소
        public int HostPort { get; set; } // 호스트 포트 번호
        public Socket ClientSocket { get; set; } // 클라이언트 소켓

        public string Role { get; set; } // 클라이언트 역할 (예: Detector)
        public string ClientName { get; set; } // 클라이언트 이름
        public string AuthToken { get; set; } // 인증 토큰 (Phase 2 추가)

        public DateTime LastSeenUtc { get; set; }
        public SessionState State { get; set; }
        public string ReconnectToken { get; set; }
        public P2PStatus P2PState { get; set; }
        public System.Net.EndPoint? UdpEndPoint { get; set; } // UDP 공인 IP/Port 정보

        // 보안 기능 연동 필드 (Milestone 2)
        public int CurrentSecondPacketCount { get; set; }
        public DateTime LastPacketTime { get; set; }
        public uint LastSequenceNumber { get; set; }
        public bool IsAuthenticated { get; set; }

        // P2P 품질 측정 필드 (Milestone 3)
        public long RttMs { get; set; }
        public double PacketLossRate { get; set; }
        public DateTime LastPingUtc { get; set; }

        // Lag Compensation 연동 필드 (Milestone 8)
        public TeruTeruServer.SDK.GameEngine.RttTracker RttHistory { get; set; } = new(10);

        // Anti-Cheat 필드 (Milestone 10)
        public int ViolationCount { get; set; }
        public int BanLevel { get; set; }  // 0=정상, 1=경고, 2=임시차단, 3=영구차단
        public DateTime LastViolationUtc { get; set; }
        public int InputCountThisTick { get; set; }
        public long LastInputTick { get; set; }

        public void UpdateRtt(long currentRttMs)
        {
            RttMs = RttHistory.AddSample(currentRttMs);
            LastPingUtc = DateTime.UtcNow;
        }

        public ClientSession(int hostID, Socket clientSocket, string gameID)
        {
            HostID = hostID;
            GameID = gameID;

            this.ClientSocket = clientSocket;
            this.LastSeenUtc = DateTime.UtcNow;
            this.State = SessionState.Connected;
            this.P2PState = P2PStatus.Signaling;
            this.ReconnectToken = Guid.NewGuid().ToString("N");

            Clear();

            if (hostID != ServerMemory.SERVER_HOST_ID)
            {
                ServerMemory.AddHostToDictionary(hostID, this);
                if (!string.IsNullOrEmpty(gameID))
                    ServerMemory.AddGameIDToDictionary(gameID, hostID);
            }
        }

        public void UpdateLastSeen()
        {
            LastSeenUtc = DateTime.UtcNow;
        }

        public void Clear()
        {


        }
    }
}