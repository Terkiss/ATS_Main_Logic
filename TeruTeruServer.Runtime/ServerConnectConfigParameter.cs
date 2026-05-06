using TeruTeruServer.SDK.Interfaces;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Runtime
{
    /// <summary>
    /// 서버 연결 설정을 저장하는 파라미터 클래스입니다.
    /// </summary>
    public class ServerConnectConfigParameter
    {
        public string IP { get; set; } = "127.0.0.1"; // 서버 IP 주소
        public int Port { get; set; } = 3000; // 서버 포트 번호
        public int MaxConnection { get; set; } = 1000; // 최대 연결 수

        public ServerConnectConfigParameter()
        {
            IsTcp = true;
            IsUdp = false;
            SendBufferSize = 8192;
            ReceiveBufferSize = 8192;
            Guid = System.Guid.NewGuid().ToString();
        }

        public enum NetworkType
        {
            None,
            TCP,
            UDP
        }
        /// <summary>
        /// 네트워크 프로토콜 타입 설정 (UDP/TCP)
        /// </summary>
        public bool IsTcp
        {
            get;
            set;
        }
        public bool IsUdp
        {
            get;
            set;
        }
        public int SendBufferSize
        {
            get;
            set;
        }
        public int ReceiveBufferSize
        {
            get;
            set;
        }
        public string Guid
        {
            get;
            set;
        }

        public void SetIP(string ip)
        {
            this.IP = ip;
        }

        public void SetPort(int port)
        {
            this.Port = port;
        }
        public void SetMaxConnection(int maxConnection)
        {
            this.MaxConnection = maxConnection;
        }


        public void SetNetworkType(NetworkType type)
        {
            if (type == NetworkType.None)
            {
                throw new NullReferenceException();
            }

            this.IsTcp = (type == NetworkType.TCP) ? true : false;
        }


        public bool SetTcp(bool isTcp)
        {
            this.IsTcp = isTcp;
            return this.IsTcp;
        }
        public bool SetUdp(bool isUdp)
        {
            this.IsUdp = isUdp;
            return this.IsUdp;
        }

        public override string ToString()
        {
            string answer = "";
            answer += $"ip={IP}\n"; 
            answer += $"guid={Guid}\n";
            answer += $"port={Port}\n";
            answer += $"max_connection={MaxConnection}\n";
            answer += $"isUdp={IsUdp}\n";
            answer += $"isTcp={IsTcp}\n";

            return answer;
        }


    }
}
