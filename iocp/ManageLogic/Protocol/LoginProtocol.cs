using System;

namespace TeruTeruServer.ManageLogic.Protocol
{
    /// <summary>
    /// 로그인 프로토콜 클래스
    /// </summary>
    public class LoginProtocol : BaseProtocol
    {
        private ProtocolSelect protocolSelector;
        private int command;
        private int hostId;

        public override ProtocolSelect ProtocolSelector
        {
            get => protocolSelector;
            set => protocolSelector = value;
        }

        public override int Command
        {
            get => command;
            set => command = value;
        }

        public override int HostId
        {
            get => hostId;
            set => hostId = value;
        }

        public string UserId { get; set; }
        public string Password { get; set; }
        public string AuthToken { get; set; }
        public bool IsSuccess { get; set; }

        public LoginProtocol()
        {
            protocolSelector = ProtocolSelect.LoginProtocol;
        }
    }
}
