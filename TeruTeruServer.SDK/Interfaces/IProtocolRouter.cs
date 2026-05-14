using System.Net.Sockets;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 플러그인이 직접 프로토콜(수동/자동)을 라우팅할 수 있도록 지원하는 라우터 인터페이스입니다.
    /// </summary>
    public interface IProtocolRouter
    {
        /// <summary>
        /// 플러그인(LogicService) 인스턴스를 분석하여 라우팅 지도를 생성합니다.
        /// </summary>
        void Initialize(ILogicService logicService);

        /// <summary>
        /// JSON 기반 프로토콜(수동 Enum 혹은 자동 RPC)을 처리합니다.
        /// </summary>
        Task<string> RouteAsync(string jsonPayload, ProtocolSelect protocol, Socket socket);
    }
}
