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
        TokenRefreshProtocol = 21,
        ReconnectProtocol = 3,
        UdpRegisterProtocol = 4,
        HolePunchRequest = 5,
        P2PRelayProtocol = 6,
        GroupRelayProtocol = 7,
        JoinGroupProtocol = 8,
        P2PPingProtocol = 9,      // P2P 연결 품질 측정용 (M3)
        RelayFallbackProtocol = 10, // 릴레이 모드 자동 전환 알림 (M3)
        StateSyncProtocol = 20,    // 서버 -> 클라이언트: Delta 상태 동기화 (M7)
        GameInputProtocol = 22,    // 클라이언트 -> 서버: 게임 입력 (M7)
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
