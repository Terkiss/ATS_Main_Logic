using System;
using System.Text;
using System.Text.Json;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Protocol;

namespace TeruTeruServer.Client
{
    /// <summary>
    /// TeruTeruServer 통신 규격에 맞는 패킷을 수동으로 구성하기 위한 유틸리티입니다.
    /// </summary>
    public static class PacketBuilder
    {
        private static uint _sequenceCounter = 1;

        /// <summary>
        /// JSON 페이로드를 포함한 6바이트 헤더 패킷을 생성합니다.
        /// [SendType(1)][ProtocolType(1)][SequenceNumber(4)][Body(N)]
        /// </summary>
        public static byte[] BuildJsonPacket(ProtocolSelect protocol, object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);
            
            byte[] packet = new byte[body.Length + 6];
            packet[0] = (byte)SendType.Json;
            packet[1] = (byte)protocol;
            
            byte[] seqBytes = BitConverter.GetBytes(_sequenceCounter++);
            Buffer.BlockCopy(seqBytes, 0, packet, 2, 4);
            Buffer.BlockCopy(body, 0, packet, 6, body.Length);
            
            return packet;
        }

        /// <summary>
        /// RPC 호출을 위한 JSON 패킷을 생성합니다.
        /// </summary>
        public static byte[] BuildRpcPacket(string methodName, object? parameters = null)
        {
            string paramJson = parameters != null ? JsonSerializer.Serialize(parameters) : "{}";
            var rpcReq = new RpcRequest { MethodName = methodName, Params = paramJson };
            
            return BuildJsonPacket(ProtocolSelect.RpcProtocol, rpcReq);
        }
    }
}
