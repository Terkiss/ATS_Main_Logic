using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Protocol
{
    /// <summary>
    /// 모든 통신 프로토콜의 기본이 되는 추상 클래스입니다.
    /// </summary>
    public abstract class BaseProtocol
    {
        /// <summary>
        /// 프로토콜 타입을 식별하기 위한 선택자입니다.
        /// </summary>
        public abstract ProtocolSelect ProtocolSelector { get; set; }

        /// <summary>
        /// 실행할 명령 구분자입니다.
        /// </summary>
        public abstract int Command { get; set; }

        /// <summary>
        /// 요청을 생성하거나 수신하는 호스트(Client/Server)의 식별자 ID입니다.
        /// </summary>
        public abstract int HostId { get; set; }
    }

    /// <summary>
    /// 로그인 및 재연결 처리를 위한 프로토콜 클래스입니다.
    /// </summary>
    public class LoginProtocol : BaseProtocol
    {
        /// <summary>
        /// 프로토콜 타입을 로그인 프로토콜로 설정합니다.
        /// </summary>
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.LoginProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        /// <summary>
        /// 사용자 아이디
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 사용자 비밀번호
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 인증용 액세스 토큰
        /// </summary>
        public string AuthToken { get; set; } = string.Empty;

        /// <summary>
        /// 액세스 토큰 만료 시 재발급을 위한 리프레시 토큰
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 클라이언트 재연결(Reconnection) 시 사용하는 토큰
        /// </summary>
        public string ReconnectToken { get; set; } = string.Empty;

        /// <summary>
        /// 로그인 또는 재연결 처리 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 기본 생성자입니다. 프로토콜 선택자를 LoginProtocol로 초기화합니다.
        /// </summary>
        public LoginProtocol()
        {
            ProtocolSelector = ProtocolSelect.LoginProtocol;
        }
    }

    /// <summary>
    /// 만료된 인증 토큰을 갱신하기 위한 프로토콜 클래스입니다.
    /// </summary>
    public class TokenRefreshProtocol : BaseProtocol
    {
        /// <summary>
        /// 프로토콜 타입을 토큰 갱신 프로토콜로 설정합니다.
        /// </summary>
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.TokenRefreshProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        /// <summary>
        /// 기존의 리프레시 토큰
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 새로 발급된 액세스 토큰
        /// </summary>
        public string NewAuthToken { get; set; } = string.Empty;

        /// <summary>
        /// 새로 발급된 리프레시 토큰
        /// </summary>
        public string NewRefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 토큰 갱신 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 기본 생성자입니다. 프로토콜 선택자를 TokenRefreshProtocol로 초기화합니다.
        /// </summary>
        public TokenRefreshProtocol()
        {
            ProtocolSelector = ProtocolSelect.TokenRefreshProtocol;
        }
    }

    /// <summary>
    /// 초기 연결 수립 및 연결 상태 유지를 위한 프로토콜 클래스입니다.
    /// </summary>
    public class ConnectProtocol : BaseProtocol
    {
        /// <summary>
        /// 프로토콜 타입을 연결 프로토콜로 설정합니다.
        /// </summary>
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.ConnectProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        /// <summary>
        /// 연결의 고유 식별자 (GUID)
        /// </summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// 연결 수립 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 추가 데이터 (서버 빌드 호환성 유지용)
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// 기본 생성자입니다. 프로토콜 선택자를 ConnectProtocol로 초기화합니다.
        /// </summary>
        public ConnectProtocol()
        {
            ProtocolSelector = ProtocolSelect.ConnectProtocol;
        }
    }
}
