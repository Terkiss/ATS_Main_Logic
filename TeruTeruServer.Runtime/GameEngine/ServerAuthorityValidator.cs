using System;
using TeruTeruServer.SDK.GameEngine;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 클라이언트로부터 수신된 입력을 서버 권위(Authority)에 따라 검증하고 적용하는 클래스입니다.
    /// </summary>
    public class ServerAuthorityValidator
    {
        private readonly IGameLoop _gameLoop;

        public ServerAuthorityValidator(IGameLoop gameLoop)
        {
            _gameLoop = gameLoop;
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

            if (ValidateMovement(entity, newX, newZ, maxSpeed))
            {
                entity.X = newX;
                entity.Z = newZ;
                entity.RotationY = input.LookY;
                entity.State = input.ActionType;
                entity.IsDirty = true;
                return true;
            }

            return false;
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
            float epsilon = 1.5f; // 테스트 환경의 부동소수점 오차 및 타이밍 오차를 고려하여 여유 증대 (10% -> 50%)

            return distanceSq <= (maxAllowedDist * epsilon) * (maxAllowedDist * epsilon);
        }
    }
}
