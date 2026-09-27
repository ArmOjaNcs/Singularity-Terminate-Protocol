using System;
using UnityEngine;

namespace Gameplay.Navigation
{
    public sealed class NavigationGrid : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [SerializeField, Min(1)] private int _cellsX = 50;
        [SerializeField, Min(1)] private int _cellsY = 50;

        [Header("Blocked Cells")]
        [SerializeField] private bool[] _blockedCells;

        public float CellSize => _cellSize;
        public int CellsX => _cellsX;
        public int CellsY => _cellsY;

        private void OnValidate()
        {
            EnsureDataSize();
        }

        private void Awake()
        {
            EnsureDataSize();
        }

        public bool IsWalkable(Vector3 worldPosition)
        {
            if (!TryGetCell(worldPosition, out int x, out int y))
                return false;

            return IsWalkable(x, y);
        }

        public bool IsWalkable(Vector3 worldPosition, float radius)
        {
            Vector3 gridPosition = transform.position;

            float halfWidth = _cellsX * _cellSize * 0.5f;
            float halfHeight = _cellsY * _cellSize * 0.5f;

            if (worldPosition.x - radius < gridPosition.x - halfWidth ||
                worldPosition.x + radius > gridPosition.x + halfWidth ||
                worldPosition.y - radius < gridPosition.y - halfHeight ||
                worldPosition.y + radius > gridPosition.y + halfHeight)
                return false;

            if (!TryGetCell(worldPosition, out int centerX, out int centerY))
                return false;

            int cellRadius = Mathf.CeilToInt(radius / _cellSize);

            for (int x = centerX - cellRadius; x <= centerX + cellRadius; x++)
            {
                for (int y = centerY - cellRadius; y <= centerY + cellRadius; y++)
                {
                    if (!IsInside(x, y))
                        return false;

                    if (!IsWalkable(x, y) && IsCircleOverlappingCell(worldPosition, radius, x, y))
                        return false;
                }
            }

            return true;
        }

        public bool IsWalkable(int x, int y)
        {
            if (!IsInside(x, y))
                return false;

            return !_blockedCells[GetIndex(x, y)];
        }

        public bool IsBlocked(int x, int y)
        {
            if (!IsInside(x, y))
                return true;

            return _blockedCells[GetIndex(x, y)];
        }

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (!IsInside(x, y))
                return;

            _blockedCells[GetIndex(x, y)] = blocked;
        }

        public bool TryGetCell(Vector3 worldPosition, out int x, out int y)
        {
            Vector3 gridPosition = transform.position;

            x = Mathf.FloorToInt((worldPosition.x - gridPosition.x) / _cellSize + _cellsX * 0.5f);
            y = Mathf.FloorToInt((worldPosition.y - gridPosition.y) / _cellSize + _cellsY * 0.5f);

            return IsInside(x, y);
        }

        public Vector3 GetCellCenter(int x, int y)
        {
            if (!IsInside(x, y))
                throw new ArgumentOutOfRangeException();

            Vector3 gridPosition = transform.position;

            return new Vector3(
                gridPosition.x + (x + 0.5f - _cellsX * 0.5f) * _cellSize,
                gridPosition.y + (y + 0.5f - _cellsY * 0.5f) * _cellSize,
                gridPosition.z);
        }

        public Vector3 GetGridSize()
        {
            return new Vector3(_cellsX * _cellSize, _cellsY * _cellSize, 0f);
        }

        private bool IsCircleOverlappingCell(Vector3 worldPosition, float radius, int x, int y)
        {
            Vector3 gridPosition = transform.position;

            float minX = gridPosition.x + (x - _cellsX * 0.5f) * _cellSize;
            float maxX = minX + _cellSize;
            float minY = gridPosition.y + (y - _cellsY * 0.5f) * _cellSize;
            float maxY = minY + _cellSize;

            float closestX = Mathf.Clamp(worldPosition.x, minX, maxX);
            float closestY = Mathf.Clamp(worldPosition.y, minY, maxY);

            float dx = worldPosition.x - closestX;
            float dy = worldPosition.y - closestY;

            return dx * dx + dy * dy <= radius * radius;
        }

        private bool IsInside(int x, int y)
        {
            return x >= 0 && x < _cellsX && y >= 0 && y < _cellsY;
        }

        private int GetIndex(int x, int y)
        {
            return y * _cellsX + x;
        }

        private void EnsureDataSize()
        {
            int requiredSize = _cellsX * _cellsY;

            if (_blockedCells != null && _blockedCells.Length == requiredSize)
                return;

            bool[] oldData = _blockedCells;

            _blockedCells = new bool[requiredSize];

            if (oldData == null)
                return;

            int copyLength = Mathf.Min(oldData.Length, _blockedCells.Length);
            Array.Copy(oldData, _blockedCells, copyLength);
        }
    }
}