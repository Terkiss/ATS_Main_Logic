using System.Net.Sockets;
using TeruTeruServer.Common.Protocol;
using TeruTeruServer.Common.Enums;

namespace TeruTeruServer.ManageLogic
{
    public interface ILogicService
    {
        void ProcessDirectProtocol(byte[] buffer, Socket socket);
        void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket);
    }
}
