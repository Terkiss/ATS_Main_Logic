using System;
using System.Collections.Generic;
using System.Linq;
using TeruTeruServer.SDK.GameEngine;

namespace TeruTeruServer.Client
{
    /// <summary>
    /// 수신한 WorldState 스냅샷들을 버퍼링하고 특정 렌더링 시간점에 맞춰 플레이어/엔티티들의 위치를 보간(Interpolation)해주는 고수준 헬퍼 클래스입니다.
    /// </summary>
    public class SnapshotInterpolator
    {
        private readonly List<WorldState> _snapshots = new();
        private readonly object _lock = new();
        private readonly int _maxBufferSize;

        private class WorldStateComparer : IComparer<WorldState>
        {
            public static readonly WorldStateComparer Instance = new();
            public int Compare(WorldState? x, WorldState? y)
            {
                if (x == null || y == null) return 0;
                return x.Timestamp.CompareTo(y.Timestamp);
            }
        }

        public SnapshotInterpolator(int maxBufferSize = 100)
        {
            _maxBufferSize = maxBufferSize;
        }

        /// <summary>
        /// 새로운 WorldState 스냅샷을 버퍼에 추가하고 시간 순서대로 정렬합니다.
        /// </summary>
        public void AddSnapshot(WorldState snapshot)
        {
            if (snapshot == null) return;

            lock (_lock)
            {
                // 중복된 틱 번호가 이미 버퍼에 있는지 확인하여 무시
                if (_snapshots.Any(s => s.TickNumber == snapshot.TickNumber))
                    return;

                // 이진 검색으로 삽입 위치 탐색
                int index = _snapshots.BinarySearch(snapshot, WorldStateComparer.Instance);
                if (index >= 0)
                {
                    // 동일한 타임스탬프의 스냅샷이 이미 존재하면 무시
                    return;
                }

                int insertIndex = ~index;
                _snapshots.Insert(insertIndex, snapshot.DeepClone());

                // 버퍼가 최대 크기를 초과하면 가장 오래된 스냅샷 제거
                while (_snapshots.Count > _maxBufferSize)
                {
                    _snapshots.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 스냅샷 버퍼를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _snapshots.Clear();
            }
        }

        /// <summary>
        /// 지정한 렌더링 시간에서 보간 딜레이를 뺀 타겟 시간에 알맞게 엔티티들의 보간 상태를 반환합니다.
        /// </summary>
        public WorldState? Interpolate(DateTime renderTime, double interpolationDelayMs)
        {
            var targetTime = renderTime.AddMilliseconds(-interpolationDelayMs);

            lock (_lock)
            {
                if (_snapshots.Count == 0) return null;
                if (_snapshots.Count == 1) return _snapshots[0].DeepClone();

                var targetState = new WorldState { Timestamp = targetTime };
                int index = _snapshots.BinarySearch(targetState, WorldStateComparer.Instance);

                int leftIndex;
                int rightIndex;

                if (index >= 0)
                {
                    leftIndex = index;
                    rightIndex = index;
                }
                else
                {
                    rightIndex = ~index;
                    leftIndex = rightIndex - 1;
                }

                // 타겟 시간 이전의 스냅샷이 없다면 가장 첫 스냅샷 반환
                if (leftIndex < 0)
                {
                    return _snapshots[0].DeepClone();
                }

                // 타겟 시간 이후의 스냅샷이 없다면 가장 마지막 스냅샷 반환 (보간 불가, 외삽 대신 최종 상태로 대체)
                if (rightIndex >= _snapshots.Count)
                {
                    return _snapshots[_snapshots.Count - 1].DeepClone();
                }

                var left = _snapshots[leftIndex];
                var right = _snapshots[rightIndex];

                if (leftIndex == rightIndex)
                {
                    return left.DeepClone();
                }

                // left와 right 스냅샷 사이에서 선형 보간 수행
                var interpolatedState = new WorldState
                {
                    TickNumber = left.TickNumber,
                    Timestamp = targetTime
                };

                double totalDuration = (right.Timestamp - left.Timestamp).TotalMilliseconds;
                float t = totalDuration > 0 
                    ? (float)((targetTime - left.Timestamp).TotalMilliseconds / totalDuration) 
                    : 0f;
                t = Math.Clamp(t, 0f, 1f);

                foreach (var leftKvp in left.Entities)
                {
                    int entityId = leftKvp.Key;
                    GameEntity leftEntity = leftKvp.Value;

                    if (right.Entities.TryGetValue(entityId, out var rightEntity))
                    {
                        var interpolatedEntity = leftEntity.DeepClone();
                        interpolatedEntity.X = Lerp(leftEntity.X, rightEntity.X, t);
                        interpolatedEntity.Y = Lerp(leftEntity.Y, rightEntity.Y, t);
                        interpolatedEntity.Z = Lerp(leftEntity.Z, rightEntity.Z, t);
                        interpolatedEntity.RotationY = LerpAngle(leftEntity.RotationY, rightEntity.RotationY, t);
                        interpolatedEntity.VelocityX = Lerp(leftEntity.VelocityX, rightEntity.VelocityX, t);
                        interpolatedEntity.VelocityZ = Lerp(leftEntity.VelocityZ, rightEntity.VelocityZ, t);
                        
                        interpolatedState.Entities.TryAdd(entityId, interpolatedEntity);
                    }
                    else
                    {
                        // 오른쪽 스냅샷에 존재하지 않는 경우 왼쪽 상태 그대로 적용
                        interpolatedState.Entities.TryAdd(entityId, leftEntity.DeepClone());
                    }
                }

                return interpolatedState;
            }
        }

        private static float Lerp(float start, float end, float t)
        {
            return start + (end - start) * t;
        }

        private static float LerpAngle(float start, float end, float t)
        {
            // 각도 최단 경로 보간 (Wrapping 방지)
            float diff = (end - start + 180) % 360 - 180;
            if (diff < -180) diff += 360;
            return start + diff * t;
        }
    }
}
