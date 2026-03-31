using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Pipeline
{
    /// <summary>
    /// 패킷 처리의 맥락을 담고 있는 클래스입니다.
    /// </summary>
    public class PacketContext
    {
        public Socket ClientSocket { get; }
        public byte[] RawData { get; set; }
        public ClientSession Session { get; set; }
        public bool IsProcessed { get; set; } = false;

        public PacketContext(Socket socket, byte[] data)
        {
            ClientSocket = socket;
            RawData = data;
        }
    }

    /// <summary>
    /// 패킷 처리를 위한 미들웨어 인터페이스입니다.
    /// </summary>
    public interface IPacketMiddleware
    {
        Task InvokeAsync(PacketContext context, Func<Task> next);
    }
}
