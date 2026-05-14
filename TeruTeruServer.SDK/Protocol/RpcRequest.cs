using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Protocol
{
    /// <summary>
    /// RPC 호출을 위한 전송 모델입니다.
    /// </summary>
    public class RpcRequest : BaseProtocol
    {
        public override ProtocolSelect ProtocolSelector { get; set; } = ProtocolSelect.RpcProtocol;
        public override int Command { get; set; }
        public override int HostId { get; set; }

        /// <summary>
        /// 호출할 RPC 메서드 이름 (RpcAttribute에 정의된 이름 혹은 메서드 명)
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 메서드 매개변수 데이터 (JSON 직렬화된 문자열)
        /// </summary>
        public string Params { get; set; }

        public RpcRequest()
        {
            ProtocolSelector = ProtocolSelect.RpcProtocol;
        }
    }
}
