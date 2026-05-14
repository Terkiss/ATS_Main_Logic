using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 스레드 세이프한 입력을 관리하는 제네릭 큐 클래스입니다.
    /// </summary>
    public class InputQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();

        /// <summary>
        /// 새로운 데이터를 큐에 추가합니다. (IOCP 수신 스레드 등에서 호출)
        /// </summary>
        public void Enqueue(T input)
        {
            _queue.Enqueue(input);
        }

        /// <summary>
        /// 현재 큐에 쌓인 모든 데이터를 한 번에 꺼내어 반환합니다. (Tick 스레드에서 호출)
        /// </summary>
        public List<T> DrainAll()
        {
            var result = new List<T>();
            while (_queue.TryDequeue(out var input))
            {
                result.Add(input);
            }
            return result;
        }

        /// <summary>
        /// 현재 큐에 쌓여있는 항목의 개수
        /// </summary>
        public int Count => _queue.Count;
    }
}
