using TeruTeruServer.ServerEngineSDK.Enums;

namespace TeruTeruServer.ServerEngineSDK.Protocol
{
    public abstract class BaseProtocol
    {
        public abstract ProtocolSelect ProtocolSelector { get; set; }
        public abstract int Command { get; set; }
        public abstract int HostId { get; set; }
    }

    public class LoginProtocol : BaseProtocol
    {
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.LoginProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        public string UserId { get; set; }
        public string Password { get; set; }
        public string AuthToken { get; set; }
        public bool IsSuccess { get; set; }

        public LoginProtocol()
        {
            ProtocolSelector = ProtocolSelect.LoginProtocol;
        }
    }

    public class ConnectProtocol : BaseProtocol
    {
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.ConnectProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        public string Guid { get; set; }
        public bool IsSuccess { get; set; }
        public string Data { get; set; } // 서버 빌드 에러 대응

        public ConnectProtocol()
        {
            ProtocolSelector = ProtocolSelect.ConnectProtocol;
        }
    }
}
