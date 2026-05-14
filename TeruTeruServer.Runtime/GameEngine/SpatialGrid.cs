using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Runtime.GameEngine
{
    /// <summary>
    /// 그리드 기반 공간 분할을 사용하여 AoI(Area of Interest)를 처리하는 클래스입니다.
    /// </summary>
    public class SpatialGrid : IAoIFilter
    {
        private readonly float _cellSize;
        
        // 키: (zoneId, cellX, cellZ), 값: HashSet<int> (entityIds)
        private readonly ConcurrentDictionary<(int, int, int), HashSet<int>> _cells = new();
        
        // 엔티티가 현재 속한 셀을 추적 (zoneId, entityId) -> (cellX, cellZ)
        private readonly ConcurrentDictionary<(int, int), (int, int)> _entityToCell = new();

        public SpatialGrid(float cellSize = 50f)
        {
            _cellSize = cellSize;
        }

        public (int cellX, int cellZ) GetCellCoord(float x, float z)
        {
            return ((int)Math.Floor(x / _cellSize), (int)Math.Floor(z / _cellSize));
        }

        public void UpdateEntityPosition(int zoneId, int entityId, float x, float z)
        {
            var newCoord = GetCellCoord(x, z);
            var entityKey = (zoneId, entityId);

            if (_entityToCell.TryGetValue(entityKey, out var oldCoord))
            {
                if (oldCoord == newCoord) return; // 셀 변화 없음

                // 이전 셀에서 제거
                RemoveFromCell(zoneId, oldCoord.Item1, oldCoord.Item2, entityId);
            }

            // 새 셀에 추가
            AddToCell(zoneId, newCoord.cellX, newCoord.cellZ, entityId);
            _entityToCell[entityKey] = newCoord;
        }

        public void RemoveEntity(int zoneId, int entityId)
        {
            if (_entityToCell.TryRemove((zoneId, entityId), out var coord))
            {
                RemoveFromCell(zoneId, coord.Item1, coord.Item2, entityId);
            }
        }

        public List<int> GetNearbyEntityIds(int zoneId, float x, float z, float radius)
        {
            var center = GetCellCoord(x, z);
            var result = new List<int>();

            // 3x3 인접 셀 순회
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int cx = center.cellX + dx;
                    int cz = center.cellZ + dz;

                    if (_cells.TryGetValue((zoneId, cx, cz), out var cellEntities))
                    {
                        lock (cellEntities)
                        {
                            result.AddRange(cellEntities);
                        }
                    }
                }
            }

            return result;
        }

        private void AddToCell(int zoneId, int cx, int cz, int entityId)
        {
            var cellKey = (zoneId, cx, cz);
            var cell = _cells.GetOrAdd(cellKey, _ => new HashSet<int>());
            lock (cell)
            {
                cell.Add(entityId);
            }
        }

        private void RemoveFromCell(int zoneId, int cx, int cz, int entityId)
        {
            var cellKey = (zoneId, cx, cz);
            if (_cells.TryGetValue(cellKey, out var cell))
            {
                lock (cell)
                {
                    cell.Remove(entityId);
                }
            }
        }
    }
}
