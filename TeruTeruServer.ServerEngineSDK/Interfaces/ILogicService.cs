using System.Net.Sockets;
using TeruTeruServer.ServerEngineSDK.Enums;

namespace TeruTeruServer.ServerEngineSDK.Interfaces
{
    /// <summary>
    /// 서버의 비즈니스 로직을 담당하는 플러그인 인터페이스입니다.
    /// </summary>
    public interface ILogicService
    {
        void ProcessDirectProtocol(byte[] buffer, Socket socket);
        void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket);
    }
}
