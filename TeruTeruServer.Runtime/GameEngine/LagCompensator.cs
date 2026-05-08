using System;
using System.Numerics;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 클라이언트의 지연 시간을 고려하여 과거 상태로 히트 판정을 수행하는 클래스입니다.
    /// </summary>
    public class LagCompensator
    {
        private readonly SnapshotBuffer _snapshotBuffer;
        private readonly IGameLoop _gameLoop;
        private readonly int _maxRewindTicks;

        public LagCompensator(SnapshotBuffer buffer, IGameLoop gameLoop, int maxRewindTicks = 40)
        {
            _snapshotBuffer = buffer;
            _gameLoop = gameLoop;
            _maxRewindTicks = maxRewindTicks;
        }

        /// <summary>
        /// RTT를 기반으로 되감기할 틱 수를 계산합니다.
        /// </summary>
        public int CalculateRewindTicks(long rttMs)
        {
            double tickIntervalMs = 1000.0 / _gameLoop.TickRate;
            // RTT의 절반(편도 지연)을 틱 단위로 환산
            return (int)Math.Round((rttMs / 2.0) / tickIntervalMs);
        }

        /// <summary>
        /// 지연 보상이 적용된 히트 판정을 수행합니다.
        /// </summary>
        public HitValidationResult ValidateHit(HitValidationRequest request, long shooterRttMs)
        {
            long currentTick = _gameLoop.CurrentTick;
            int rewindCount = CalculateRewindTicks(shooterRttMs);
            
            // 최대 되감기 제한 적용 (치트 방지)
            rewindCount = Math.Min(rewindCount, _maxRewindTicks);
            long targetTick = currentTick - rewindCount;

            var result = new HitValidationResult
            {
                ServerTick = currentTick,
                RewindTick = targetTick,
                TargetEntityId = request.TargetEntityId
            };

            // 과거 스냅샷 조회
            var historyState = _snapshotBuffer.GetAtTick(targetTick);
            if (historyState == null || !historyState.Entities.TryGetValue(request.TargetEntityId, out var targetEntity))
            {
                result.IsHit = false;
                return result;
            }

            // 레이-구체 충돌 판정
            // 슈터 위치 S, 에임 방향 D (정규화), 타겟 위치 T, 반경 R
            Vector3 S = new Vector3(request.ShooterX, request.ShooterY, request.ShooterZ);
            Vector3 D = Vector3.Normalize(new Vector3(request.AimX, request.AimY, request.AimZ));
            Vector3 T = new Vector3(targetEntity.X, targetEntity.Y, targetEntity.Z);
            float R = targetEntity.HitboxRadius;

            Vector3 f = S - T;
            
            // 최단 거리(Distance) 계산: 레이 직선과 점 T 사이의 거리
            // d = |(T-S) x D| / |D| , D는 정규화되었으므로 |D|=1
            Vector3 TS = T - S;
            float crossProductLen = Vector3.Cross(TS, D).Length();
            result.Distance = crossProductLen;

            // 충돌 판정 (discriminant 공식 활용 가능하나, 여기선 거리 기반으로 단순화)
            // 레이가 구체 방향을 향하고 있는지(내적)와 최단 거리가 반경 이내인지 확인
            float dotProduct = Vector3.Dot(TS, D);
            if (dotProduct > 0 && crossProductLen <= R)
            {
                result.IsHit = true;
            }
            else
            {
                result.IsHit = false;
            }

            return result;
        }
    }
}
