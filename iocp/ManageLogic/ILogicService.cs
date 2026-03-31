using System.Net.Sockets;
using TeruTeruServer.ManageLogic.Protocol;

namespace TeruTeruServer.ManageLogic
{
    public interface ILogicService
    {
        void ProcessDirectProtocol(byte[] buffer, Socket socket);
        void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket);
    }
}
