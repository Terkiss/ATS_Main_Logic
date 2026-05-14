using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 클라이언트로부터 수신된 입력을 서버 권위(Authority)에 따라 검증하고 적용하는 클래스입니다.
    /// </summary>
    public class ServerAuthorityValidator
    {
        private readonly IGameLoop _gameLoop;
        private readonly ISecurityEventLogger _securityLogger;
        private readonly SanctionManager _sanctionManager;

        // 세션별 최근 N틱 위치 히스토리 (L90-92 지시사항 준수)
        private readonly ConcurrentDictionary<int, Queue<(float x, float z, long tick)>> _positionHistory = new();
        private const int HISTORY_SIZE = 60; // 3초분 (20Hz)

        public ServerAuthorityValidator(IGameLoop gameLoop, ISecurityEventLogger securityLogger, SanctionManager sanctionManager)
        {
            _gameLoop = gameLoop;
            _securityLogger = securityLogger;
            _sanctionManager = sanctionManager;
        }

        /// <summary>
        /// 클라이언트 이동 입력을 검증하고 엔티티 상태에 적용합니다.
        /// </summary>
        public bool ValidateAndApply(GameInput input, GameEntity entity, float maxSpeed)
        {
            // 1. 입력 클램프 (최대 속도 초과 방지)
            float moveX = Math.Clamp(input.MoveX, -maxSpeed, maxSpeed);
            float moveZ = Math.Clamp(input.MoveZ, -maxSpeed, maxSpeed);

            // 2. 물리적 이동 거리 검증
            float tickInterval = 1.0f / _gameLoop.TickRate;
            float newX = entity.X + (moveX * tickInterval);
            float newZ = entity.Z + (moveZ * tickInterval);

            bool isValid = ValidateMovement(entity, newX, newZ, maxSpeed);
            
            if (isValid)
            {
                entity.X = newX;
                entity.Z = newZ;
                entity.RotationY = input.LookY;
                entity.State = input.ActionType;
                entity.IsDirty = true;

                // 경로 적분 검증 수행 (L115)
                var secEvt = ValidatePathIntegrity(input.HostId, entity, maxSpeed);
                if (secEvt != null)
                {
                    var session = ServerMemory.FindClientSession(input.HostId);
                    if (session != null)
                    {
                        _sanctionManager.ProcessViolation(session, secEvt);
                    }
                }

                return true;
            }
            else
            {
                // 단일 틱 검증 실패 시 SecurityEvent 발행 (L39)
                var session = ServerMemory.FindClientSession(input.HostId);
                if (session != null)
                {
                    _sanctionManager.ProcessViolation(session, new SecurityEvent
                    {
                        HostId = input.HostId,
                        EventType = "MovementViolation",
                        Description = $"Single tick movement exceeds limit at tick {_gameLoop.CurrentTick}",
                        Severity = "Warning"
                    });
                }
                return false;
            }
        }

        /// <summary>
        /// 이동 거리가 틱 간격 내에서 물리적으로 가능한지 검증합니다.
        /// </summary>
        public bool ValidateMovement(GameEntity entity, float newX, float newZ, float maxSpeed)
        {
            float dx = newX - entity.X;
            float dz = newZ - entity.Z;
            float distanceSq = dx * dx + dz * dz;

            // 1틱당 이동 가능한 최대 거리 (약간의 오차 허용)
            float tickInterval = 1.0f / _gameLoop.TickRate;
            float maxAllowedDist = maxSpeed * tickInterval;
            float epsilon = 1.5f; // L449 지시사항 준수: 기존 epsilon=1.5f 유지

            return distanceSq <= (maxAllowedDist * epsilon) * (maxAllowedDist * epsilon);
        }

        /// <summary>
        /// 경로 적분(Path Integration) 및 텔레포트 검증을 수행합니다. (L88-103)
        /// </summary>
        public SecurityEvent? ValidatePathIntegrity(int hostId, GameEntity entity, float maxSpeed)
        {
            long currentTick = _gameLoop.CurrentTick;
            float currentX = entity.X;
            float currentZ = entity.Z;

            var history = _positionHistory.GetOrAdd(hostId, _ => new Queue<(float x, float z, long tick)>());

            lock (history)
            {
                // 1. 히스토리에 현재 위치 추가
                history.Enqueue((currentX, currentZ, currentTick));
                if (history.Count > HISTORY_SIZE)
                    history.Dequeue();

                if (history.Count < 2) return null;

                // 5. 텔레포트 감지: 단일 틱 이동 거리가 maxSpeed * tickInterval * 5.0f 초과 시 (L100)
                var last = history.ElementAt(history.Count - 2);
                float dx = currentX - last.x;
                float dz = currentZ - last.z;
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                float tickInterval = 1.0f / _gameLoop.TickRate;

                if (dist > maxSpeed * tickInterval * 5.0f)
                {
                    return new SecurityEvent
                    {
                        HostId = hostId,
                        EventType = "Teleport",
                        Description = $"Sudden jump of {dist:F2}m detected at tick {currentTick}",
                        Severity = "Critical"
                    };
                }

                // 2. 최근 HISTORY_SIZE 틱 동안의 누적 이동 거리 계산 (L97)
                float totalDistance = 0;
                var array = history.ToArray();
                for (int i = 1; i < array.Length; i++)
                {
                    float dxi = array[i].x - array[i - 1].x;
                    float dzi = array[i].z - array[i - 1].z;
                    totalDistance += (float)Math.Sqrt(dxi * dxi + dzi * dzi);
                }

                // 3. maxSpeed * elapsed ticks * tickInterval * epsilon(2.0f)과 비교 (L98)
                long elapsedTicks = currentTick - array[0].tick;
                if (elapsedTicks <= 0) return null;

                float maxAllowedDist = maxSpeed * elapsedTicks * tickInterval * 2.0f;
                if (totalDistance > maxAllowedDist)
                {
                    return new SecurityEvent
                    {
                        HostId = hostId,
                        EventType = "SpeedHack",
                        Description = $"Accumulated distance {totalDistance:F2}m exceeds limit {maxAllowedDist:F2}m over {elapsedTicks} ticks",
                        Severity = "Warning"
                    };
                }
            }
            return null;
        }

        public void RemoveSession(int hostId)
        {
            _positionHistory.TryRemove(hostId, out _);
        }
    }
}
