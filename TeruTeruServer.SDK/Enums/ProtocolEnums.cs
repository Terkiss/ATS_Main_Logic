namespace TeruTeruServer.SDK.Enums
{
    public enum SendType : byte
    {
        Direct = 0,
        Json = 1,
    }

    public enum ProtocolSelect : byte
    {
        ConnectProtocol = 1,
        LoginProtocol = 2,
        ReconnectProtocol = 3,
        UdpRegisterProtocol = 4,
        HolePunchRequest = 5,
        P2PRelayProtocol = 6,
        GroupRelayProtocol = 7,
        JoinGroupProtocol = 8,
        RpcProtocol = 100,       // 범용 RPC 프로토콜 추가
        QueueCountCommand = 101, // 큐 카운트 요청 (Phase 3 기능 대비)
        ImageDumpCommand = 102   // 이미지 덤프 요청 (Phase 3 기능 대비)
    }

    public enum SessionState
    {
        Connected,
        Grace,
        Disconnected
    }

    public enum P2PStatus
    {
        Signaling,
        Direct,
        Relay
    }
}
