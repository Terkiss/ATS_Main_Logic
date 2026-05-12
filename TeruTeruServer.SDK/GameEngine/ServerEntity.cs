namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 서버 관할 NPC 및 몬스터를 나타내는 엔티티 모델입니다.
    /// </summary>
    public class ServerEntity : GameEntity
    {
        /// <summary>
        /// AI 행동 상태 (Idle, Patrol, Chase, Attack 등)
        /// </summary>
        public string AiBehavior { get; set; } = "Idle";

        public float PatrolRadius { get; set; } = 10f;
        public float SpawnX { get; set; }
        public float SpawnZ { get; set; }

        /// <summary>
        /// 추적 대상의 HostID (-1이면 없음)
        /// </summary>
        public int TargetHostId { get; set; } = -1;

        /// <summary>
        /// 어그로 인식 범위
        /// </summary>
        public float AggroRange { get; set; } = 15f;

        public ServerEntity()
        {
            OwnerHostId = -1; // 서버 관할 엔티티임을 명시
        }

        public override GameEntity DeepClone()
        {
            var clone = new ServerEntity
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
                HitboxRadius = this.HitboxRadius,
                
                // ServerEntity 전용 필드 복사
                AiBehavior = this.AiBehavior,
                PatrolRadius = this.PatrolRadius,
                SpawnX = this.SpawnX,
                SpawnZ = this.SpawnZ,
                TargetHostId = this.TargetHostId,
                AggroRange = this.AggroRange
            };
            return clone;
        }
    }
}
