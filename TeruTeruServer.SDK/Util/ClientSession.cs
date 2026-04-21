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

        public ClientSession(int hostID, Socket clientSocket, string gameID)
        {
            HostID = hostID;
            GameID = gameID;

            this.ClientSocket = clientSocket;
            Clear();

            ServerMemory.AddHostToDictionary(hostID, this);
            ServerMemory.AddGameIDToDictionary(gameID, hostID);
        }

        public void Clear()
        {


        }
    }
}