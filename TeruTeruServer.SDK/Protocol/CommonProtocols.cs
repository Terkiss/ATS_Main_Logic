using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Protocol
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

        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public LoginProtocol()
        {
            ProtocolSelector = ProtocolSelect.LoginProtocol;
        }
    }

    public class TokenRefreshProtocol : BaseProtocol
    {
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.TokenRefreshProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
        public string NewAuthToken { get; set; } = string.Empty;
        public string NewRefreshToken { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public TokenRefreshProtocol()
        {
            ProtocolSelector = ProtocolSelect.TokenRefreshProtocol;
        }
    }

    public class ConnectProtocol : BaseProtocol
    {
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.ConnectProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        public string Guid { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Data { get; set; } = string.Empty; // 서버 빌드 에러 대응

        public ConnectProtocol()
        {
            ProtocolSelector = ProtocolSelect.ConnectProtocol;
        }
    }
}
