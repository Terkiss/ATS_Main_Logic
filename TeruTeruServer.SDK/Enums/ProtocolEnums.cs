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
        StateAckProtocol = 23,     // 서버 -> 클라이언트: 입력 확인 + 보정 (M8)
        RttPingProtocol = 24,      // RTT 측정용 핑/퐁 (M8)
        HitValidationProtocol = 25, // 피격 판정 요청 (M8)
        ZoneTransferProtocol = 26,  // Zone 이동 요청/응답 (M9)
        ZoneInfoProtocol = 27,      // Zone 정보 조회 (M9)
        SecurityEventProtocol = 28,    // 보안 이벤트 알림 (M10)
        MatchmakingProtocol = 29,     // 매치메이킹 요청/응답 (M11)
        SessionStateProtocol = 30,    // 세션 상태 변경 알림 (M11)
        ClusterInfoProtocol = 31,      // 클러스터 상태 조회 (M12)
        DashboardProtocol = 32,        // 대시보드 데이터 요청 (M12)
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
