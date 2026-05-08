namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 서버가 클라이언트의 입력을 처리한 후 반환하는 확인 및 보정 데이터 모델입니다.
    /// CSP(Client-Side Prediction) Reconciliation에 사용됩니다.
    /// </summary>
    public class StateAck
    {
        /// <summary>
        /// 이 응답을 생성한 시점의 서버 틱 번호
        /// </summary>
        public long ServerTick { get; set; }

        /// <summary>
        /// 서버가 마지막으로 처리 완료한 클라이언트의 틱 번호
        /// </summary>
        public long LastProcessedClientTick { get; set; }

        /// <summary>
        /// 서버가 판정한 최종 위치 정보 (Authoritative Position)
        /// </summary>
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        /// <summary>
        /// 서버가 판정한 최종 속도 정보
        /// </summary>
        public float VelocityX { get; set; }
        public float VelocityZ { get; set; }
    }
}
