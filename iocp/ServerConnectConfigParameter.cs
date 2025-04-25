using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer
{
    public class ServerConnectConfigParameter
    {
        public string IP
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }
        public int MaxConnection
        {
            get;
            set;
        }

        public enum NetworkType
        {
            None,
            TCP,
            UDP
        }
        /// <summary>
        /// 0 -> udp 
        /// 1 -> tcp
        /// </summary>
        public bool isTcp
        {
            get;
            set;
        }
        public bool isUdp
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

            this.isTcp = (type == NetworkType.TCP) ? true : false;
        }


        public bool SetTcp(bool isTcp)
        {
            this.isTcp = isTcp;
            return this.isTcp;
        }
        public bool SetUdp(bool isUdp)
        {
            this.isUdp = isUdp;
            return this.isUdp;
        }

        public override string ToString()
        {
            string anser = "";
            anser += $"ip={IP}\n"; 
            anser += $"guid={Guid}\n";
            anser += $"port={Port}\n";
            anser += $"max_connection={MaxConnection}\n";
            anser += $"isUdp={isUdp}\n";
            anser += $"isTcp={isTcp}\n";

            return anser;
            
        }


    }
}
