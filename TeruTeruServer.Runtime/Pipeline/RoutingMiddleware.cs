using TeruTeruServer.SDK.Interfaces;
using System;
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

        public Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            if (buffer.Length < 2) return Task.CompletedTask;

            var sendType = (SendType)buffer[0];
            var protocolType = (ProtocolSelect)buffer[1]; // 프로토콜 타입 추출

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
                
                TeruTeruLogger.LogInfo($"Routing JSON: {protocolType}");
                _serverLogic.ProcessJsonProtocol(json, protocolType, context.ClientSocket);
            }

            context.IsProcessed = true;
            return Task.CompletedTask;
        }
    }
}
