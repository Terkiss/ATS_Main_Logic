namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 플레이어의 존 이동 요청 데이터를 담는 모델입니다.
    /// </summary>
    public class ZoneTransferRequest
    {
        public int HostId { get; set; }
        public int FromZoneId { get; set; }
        public int ToZoneId { get; set; }
        
        /// <summary>
        /// 목적지 존에서의 스폰 위치
        /// </summary>
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }
    }
}
