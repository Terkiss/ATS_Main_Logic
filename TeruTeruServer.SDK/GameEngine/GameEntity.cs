namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 게임 내 모든 동기화 대상(플레이어, NPC 등)을 나타내는 엔티티 모델입니다.
    /// </summary>
    public class GameEntity
    {
        /// <summary>
        /// 엔티티 고유 ID
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// 소유 클라이언트 HostID (-1이면 서버 관할 NPC)
        /// </summary>
        public int OwnerHostId { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        /// <summary>
        /// Y축 회전 (Yaw)
        /// </summary>
        public float RotationY { get; set; }

        public float VelocityX { get; set; }
        public float VelocityZ { get; set; }

        /// <summary>
        /// 엔티티 상태 (Idle, Moving, Attacking 등)
        /// </summary>
        public string State { get; set; } = "Idle";

        /// <summary>
        /// Delta 계산을 위한 변경 감지 플래그
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// 히트 판정용 구체 콜라이더 반경
        /// </summary>
        public float HitboxRadius { get; set; } = 0.5f;

        /// <summary>
        /// 데이터 깊은 복사 (스냅샷용)
        /// </summary>
        public virtual GameEntity DeepClone()
        {
            return new GameEntity
            {
                EntityId = this.EntityId,
                OwnerHostId = this.OwnerHostId,
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                RotationY = this.RotationY,
                VelocityX = this.VelocityX,
                VelocityZ = this.VelocityZ,
                State = this.State,
                IsDirty = this.IsDirty,
                HitboxRadius = this.HitboxRadius
            };
        }
    }
}
