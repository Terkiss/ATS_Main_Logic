using System;

namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 시스템 전역의 이벤트를 발행 및 구독하기 위한 인터페이스입니다.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 지정된 채널에 메시지를 발행합니다.
        /// </summary>
        void Publish<T>(string channel, T message);

        /// <summary>
        /// 지정된 채널의 메시지를 구독합니다.
        /// </summary>
        void Subscribe<T>(string channel, Action<T> handler);

        /// <summary>
        /// 지정된 채널의 구독을 해제합니다.
        /// </summary>
        void Unsubscribe(string channel);
    }
}
