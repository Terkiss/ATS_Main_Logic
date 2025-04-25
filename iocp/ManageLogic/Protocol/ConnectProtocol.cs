using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Protocol
{
    /// <summary>
    /// 연결 프로토콜 클래스
    /// </summary>
    public class ConnectProtocol : BaseProtocol
    {
        private ProtocolSelect protocolSelector;
        private int command;
        private int hostId;



        public override ProtocolSelect ProtocolSelector
        {
            get => protocolSelector;
            set => protocolSelector = value;
        }


        /// <summary>
        /// 프로토콜 명령 코드
        /// </summary>
        public override int Command
        {
            get => command;
            set => command = value;
        }

        /// <summary>
        /// 호스트 ID
        /// </summary>
        public override int HostId
        {
            get => hostId;
            set => hostId = value;
        }

        /// <summary>
        /// 고유한 식별자(GUID)
        /// </summary>
        public string Guid { get; set; }

        public string Data { get; set; }




        public ConnectProtocol()
        {
            protocolSelector = ProtocolSelect.ConnectProtocol;
        }







        /// <summary>
        /// 객체를 문자열로 변환하여 출력
        /// </summary>
        /// <returns>객체 정보를 포함하는 문자열</returns>
        public override string ToString()
        {
            return command.ToString() + "\n" + hostId.ToString() + "\n" + Guid + "\n" + Data;
        }
    }
}
