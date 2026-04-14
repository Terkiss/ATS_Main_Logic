using TeruTeruServer.SDK.Interfaces;
using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using TeruTeruServer.Runtime;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    public class RoutingMiddleware : IPacketMiddleware
    {
        private readonly ILogicService _serverLogic;

        public RoutingMiddleware(ILogicService serverLogic)
        {
            _serverLogic = serverLogic;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            if (buffer.Length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocolType = (ProtocolSelect)buffer[1];

            if (sendType == SendType.Direct)
            {
                byte[] data = new byte[buffer.Length - 1];
                Array.Copy(buffer, 1, data, 0, buffer.Length - 1);
                _serverLogic.ProcessDirectProtocol(data, context.ClientSocket);
            }
            else if (sendType == SendType.Json)
            {
                byte[] data = new byte[buffer.Length - 2];
                Array.Copy(buffer, 2, data, 0, buffer.Length - 2);
                string json = System.Text.Encoding.UTF8.GetString(data);
                
                // 엔진은 단순히 로직에 던져주기만 함 (라우팅 책임은 로직에 있음)
                _serverLogic.ProcessJsonProtocol(json, protocolType, context.ClientSocket);
            }

            context.IsProcessed = true;
            await Task.CompletedTask;
        }
    }
}
