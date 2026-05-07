namespace TeruTeruServer.Runtime.Rpc
{
    /// <summary>
    /// 플러그인에 등록된 엔드포인트(RPC 또는 Protocol) 정보를 담는 모델입니다.
    /// </summary>
    public class ProtocolEndpointInfo
    {
        /// <summary>
        /// 실제 구현된 메서드 이름입니다.
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// 외부에서 호출할 때 사용하는 이름 (RPC 이름 또는 Protocol Enum 이름)입니다.
        /// </summary>
        public string ProtocolOrRpcName { get; set; } = string.Empty;

        /// <summary>
        /// 바인딩 타입 ("Rpc" 또는 "Protocol")입니다.
        /// </summary>
        public string BindingType { get; set; } = string.Empty;

        /// <summary>
        /// 인증이 필요한지 여부입니다.
        /// </summary>
        public bool RequiresAuth { get; set; }
    }
}
