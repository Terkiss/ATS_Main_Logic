namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 클라이언트가 서버에 피격 판정을 요청할 때 사용하는 데이터 모델입니다.
    /// </summary>
    public class HitValidationRequest
    {
        public int ShooterHostId { get; set; }
        public int TargetEntityId { get; set; }

        /// <summary>
        /// 클라이언트가 발사(판정) 시점이라고 주장하는 틱 번호
        /// </summary>
        public long ClientTick { get; set; }

        /// <summary>
        /// 발사 위치 (슈터 위치)
        /// </summary>
        public float ShooterX { get; set; }
        public float ShooterY { get; set; }
        public float ShooterZ { get; set; }

        /// <summary>
        /// 에임 방향 (정규화된 벡터)
        /// </summary>
        public float AimX { get; set; }
        public float AimY { get; set; }
        public float AimZ { get; set; }
    }

    /// <summary>
    /// 서버의 피격 판정 결과 데이터 모델입니다.
    /// </summary>
    public class HitValidationResult
    {
        /// <summary>
        /// 최종 피격 성공 여부
        /// </summary>
        public bool IsHit { get; set; }

        public int TargetEntityId { get; set; }

        /// <summary>
        /// 판정 시점의 서버 현재 틱
        /// </summary>
        public long ServerTick { get; set; }

        /// <summary>
        /// 판정을 위해 실제로 되감은 틱 번호
        /// </summary>
        public long RewindTick { get; set; }

        /// <summary>
        /// 레이와 타겟 중심점 사이의 최단 거리
        /// </summary>
        public float Distance { get; set; }
    }
}
