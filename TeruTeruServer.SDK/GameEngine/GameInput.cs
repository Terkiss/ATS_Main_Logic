namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 클라이언트로부터 수신된 게임 입력 데이터를 나타내는 모델입니다.
    /// </summary>
    public class GameInput
    {
        /// <summary>
        /// 입력을 보낸 플레이어의 HostID
        /// </summary>
        public int HostId { get; set; }

        /// <summary>
        /// 클라이언트 기준의 틱 번호 (Reconciliation 등에 사용)
        /// </summary>
        public long ClientTick { get; set; }

        public float MoveX { get; set; }
        public float MoveZ { get; set; }

        /// <summary>
        /// 시선 방향 (Yaw)
        /// </summary>
        public float LookY { get; set; }

        /// <summary>
        /// 수행 중인 액션 유형 (Move, Attack, Jump 등)
        /// </summary>
        public string ActionType { get; set; } = "None";
    }
}
