using System;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 고정 주기로 서버의 게임 로직을 갱신하는 루프 인터페이스입니다.
    /// </summary>
    public interface IGameLoop
    {
        /// <summary>
        /// 초당 갱신 횟수 (Hz)
        /// </summary>
        int TickRate { get; }

        /// <summary>
        /// 서버 시작 이후 누적된 틱 번호
        /// </summary>
        long CurrentTick { get; }

        /// <summary>
        /// 루프 구동 여부
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 루프를 시작합니다.
        /// </summary>
        void Start();

        /// <summary>
        /// 루프를 중지합니다.
        /// </summary>
        void Stop();

        /// <summary>
        /// 매 틱마다 호출될 핸들러를 등록합니다.
        /// </summary>
        void RegisterTickHandler(Action<long> handler);

        /// <summary>
        /// 등록된 핸들러를 제거합니다.
        /// </summary>
        void UnregisterTickHandler(Action<long> handler);
    }
}
