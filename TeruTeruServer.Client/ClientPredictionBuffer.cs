using System;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.Client
{
    /// <summary>
    /// 클라이언트 예측 엔트리를 저장하는 클래스입니다.
    /// </summary>
    public class PredictionEntry
    {
        public GameInput Input { get; set; }
        public GameEntity State { get; set; }

        public PredictionEntry(GameInput input, GameEntity state)
        {
            Input = input;
            State = state;
        }
    }

    /// <summary>
    /// 클라이언트가 서버 확인(StateAck)을 기다리는 동안 적용한 게임 입력 및 예측 상태를 보관하는 예측 버퍼입니다.
    /// 서버로부터 오차가 탐지될 경우 상태 보정(Reconciliation) 및 재시뮬레이션을 지원합니다.
    /// </summary>
    public class ClientPredictionBuffer
    {
        private readonly List<PredictionEntry> _buffer = new();
        private readonly object _lock = new();

        /// <summary>
        /// 클라이언트가 서버로 송신할 입력과 그에 상응하는 로컬 예측 상태를 기록합니다.
        /// </summary>
        public void RecordInput(GameInput input, GameEntity predictedState)
        {
            if (input == null || predictedState == null) return;

            lock (_lock)
            {
                // 동일한 틱의 기존 예측 항목이 존재하면 덮어씌움
                _buffer.RemoveAll(e => e.Input.ClientTick == input.ClientTick);
                _buffer.Add(new PredictionEntry(input, predictedState.DeepClone()));
                _buffer.Sort((a, b) => a.Input.ClientTick.CompareTo(b.Input.ClientTick));
            }
        }

        /// <summary>
        /// 서버의 Authoritative 응답(StateAck)을 바탕으로 예측 오차를 검증하고, 오차가 임계값(epsilon) 이상일 시 위치 보정 및 후속 입력 재시뮬레이션을 진행합니다.
        /// </summary>
        /// <param name="ack">서버로부터 전달받은 상태 응답 패킷</param>
        /// <param name="simulationDelegate">엔티티와 입력을 받아 다음 상태를 시뮬레이션하는 대리자</param>
        /// <param name="epsilon">위치 오차 허용 임계값</param>
        /// <returns>최신 보정 완료된 최종 클라이언트 예측 상태</returns>
        public GameEntity? Reconcile(StateAck ack, Action<GameEntity, GameInput> simulationDelegate, float epsilon = 0.001f)
        {
            if (ack == null) return null;

            lock (_lock)
            {
                // 1. 서버가 마지막으로 처리한 클라이언트 틱 찾기
                PredictionEntry? ackedEntry = _buffer.FirstOrDefault(e => e.Input.ClientTick == ack.LastProcessedClientTick);

                if (ackedEntry != null)
                {
                    // 2. 서버에서 보장하는 Authoritative 위치와 예측했던 위치 비교
                    float diffX = ackedEntry.State.X - ack.X;
                    float diffY = ackedEntry.State.Y - ack.Y;
                    float diffZ = ackedEntry.State.Z - ack.Z;
                    double error = Math.Sqrt(diffX * diffX + diffY * diffY + diffZ * diffZ);

                    // 3. 오차가 임계값을 넘어가면 보정 및 재시뮬레이션 수행 (Reconciliation)
                    if (error > epsilon)
                    {
                        // 3.1. 기준 틱의 위치 및 속도를 서버가 판정한 공식 값으로 강제 스냅
                        ackedEntry.State.X = ack.X;
                        ackedEntry.State.Y = ack.Y;
                        ackedEntry.State.Z = ack.Z;
                        ackedEntry.State.VelocityX = ack.VelocityX;
                        ackedEntry.State.VelocityZ = ack.VelocityZ;

                        var currentState = ackedEntry.State.DeepClone();

                        // 3.2. 이후 버퍼에 남아있는 모든 unacknowledged 입력들을 순차적으로 다시 시뮬레이션
                        var subsequent = _buffer
                            .Where(e => e.Input.ClientTick > ack.LastProcessedClientTick)
                            .OrderBy(e => e.Input.ClientTick)
                            .ToList();

                        foreach (var entry in subsequent)
                        {
                            simulationDelegate(currentState, entry.Input);
                            entry.State = currentState.DeepClone();
                        }
                    }
                }

                // 4. 서버가 이미 승인한 이전 틱의 항목들은 버퍼에서 완전히 정리
                _buffer.RemoveAll(e => e.Input.ClientTick <= ack.LastProcessedClientTick);

                // 5. 가장 최신 예측(또는 방금 보정 완료된) 클라이언트 상태 반환
                return _buffer.LastOrDefault()?.State ?? (ackedEntry != null ? ackedEntry.State.DeepClone() : null);
            }
        }

        /// <summary>
        /// 버퍼의 모든 데이터를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }

        /// <summary>
        /// 현재 버퍼에 저장된 예측 프레임의 개수입니다.
        /// </summary>
        public int BufferCount
        {
            get
            {
                lock (_lock) return _buffer.Count;
            }
        }
    }
}
