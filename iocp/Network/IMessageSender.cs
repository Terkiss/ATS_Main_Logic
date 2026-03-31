using System.Net.Sockets;

namespace TeruTeruServer.Network
{
    /// <summary>
    /// 클라이언트에게 데이터를 전송하는 인터페이스입니다.
    /// </summary>
    public interface IMessageSender
    {
        void SendData(Socket socket, byte[] data);
        void SendData(int hostID, byte[] data);
    }
}
