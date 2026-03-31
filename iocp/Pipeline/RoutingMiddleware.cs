using System;
using System.Threading.Tasks;
using TeruTeruServer.ManageLogic;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Pipeline
{
    /// <summary>
    /// 최종 비즈니스 로직(ServerLogic)으로 패킷을 라우팅하는 미들웨어입니다.
    /// </summary>
    public class RoutingMiddleware : IPacketMiddleware
    {
        private readonly ILogicService _serverLogic;

        public RoutingMiddleware(ILogicService serverLogic)
        {
            _serverLogic = serverLogic;
        }

        public Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            var count = buffer.Length;
            var socket = context.ClientSocket;

            var sendType = (SendType)buffer[0];

            if (sendType == SendType.Direct)
            {
                byte[] data = new byte[count - 1];
                Array.Copy(buffer, 1, data, 0, count - 1);
                _serverLogic.ProcessDirectProtocol(data, socket);
            }
            else if (sendType == SendType.Json)
            {
                byte[] data = new byte[count - 1];
                Array.Copy(buffer, 1, data, 0, count - 1);
                string json = System.Text.Encoding.ASCII.GetString(data);
                TeruTeruLogger.LogInfo("Received JSON via Pipeline: " + json);
                _serverLogic.ProcessJsonProtocol(json, ProtocolSelect.ConnectProtocol, socket);
            }

            context.IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
