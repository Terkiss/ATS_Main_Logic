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
        RpcProtocol = 100,       // 범용 RPC 프로토콜 추가
        QueueCountCommand = 101, // 큐 카운트 요청 (Phase 3 기능 대비)
        ImageDumpCommand = 102   // 이미지 덤프 요청 (Phase 3 기능 대비)
    }
}
