using System;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
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

        // Legacy overload for callers that use a circular radius.
        public bool IsWalkable(Vector3 worldPosition, float radius)
        {
            radius = Mathf.Max(0f, radius);

            Vector3 gridPosition = transform.position;

            float halfWidth = _cellsX * _cellSize * 0.5f;
            float halfHeight = _cellsY * _cellSize * 0.5f;

            if (worldPosition.x - radius < gridPosition.x - halfWidth ||
                worldPosition.x + radius > gridPosition.x + halfWidth ||
                worldPosition.y - radius < gridPosition.y - halfHeight ||
                worldPosition.y + radius > gridPosition.y + halfHeight)
            {
                return false;
            }

            if (!TryGetCell(worldPosition, out int centerX, out int centerY))
                return false;

            int cellRadius = Mathf.CeilToInt(radius / _cellSize) + 1;

            for (int x = centerX - cellRadius; x <= centerX + cellRadius; x++)
            {
                for (int y = centerY - cellRadius; y <= centerY + cellRadius; y++)
                {
                    if (!IsInside(x, y))
                        return false;

                    if (!IsWalkable(x, y) &&
                        IsCircleOverlappingCell(worldPosition, radius, x, y))
                    {
                        return false;
                    }
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

            x = Mathf.FloorToInt(
                (worldPosition.x - gridPosition.x) / _cellSize +
                _cellsX * 0.5f);

            y = Mathf.FloorToInt(
                (worldPosition.y - gridPosition.y) / _cellSize +
                _cellsY * 0.5f);

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
            return new Vector3(
                _cellsX * _cellSize,
                _cellsY * _cellSize,
                0f);
        }

        public bool IsWalkable(
            Vector3 worldPosition,
            ColliderComponent collider)
        {
            GetColliderExtents(collider, out float extentX, out float extentY);

            Vector3 gridPosition = transform.position;

            float halfWidth = _cellsX * _cellSize * 0.5f;
            float halfHeight = _cellsY * _cellSize * 0.5f;

            // Check whether the collider's bounds fit inside the grid.
            if (worldPosition.x - extentX < gridPosition.x - halfWidth ||
                worldPosition.x + extentX > gridPosition.x + halfWidth ||
                worldPosition.y - extentY < gridPosition.y - halfHeight ||
                worldPosition.y + extentY > gridPosition.y + halfHeight)
            {
                return false;
            }

            if (!TryGetCell(worldPosition, out int centerX, out int centerY))
                return false;

            int radiusX = Mathf.CeilToInt(extentX / _cellSize) + 1;
            int radiusY = Mathf.CeilToInt(extentY / _cellSize) + 1;

            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                {
                    if (!IsInside(x, y) || IsWalkable(x, y))
                        continue;

                    Vector3 cellCenter = GetCellCenter(x, y);

                    ColliderComponent cellCollider = new ColliderComponent
                    {
                        Type = ColliderType.Box,
                        Size = new Vector2(_cellSize, _cellSize),
                        Angle = 0f
                    };

                    if (ColliderGeometry.Overlaps(
                            worldPosition,
                            collider,
                            cellCenter,
                            cellCollider))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void GetColliderExtents(
            ColliderComponent collider,
            out float extentX,
            out float extentY)
        {
            extentX = 0f;
            extentY = 0f;

            switch (collider.Type)
            {
                case ColliderType.Circle:
                    {
                        float radius = Mathf.Max(0f, collider.Radius);

                        extentX = radius;
                        extentY = radius;
                        break;
                    }

                case ColliderType.Box:
                    {
                        float halfWidth = Mathf.Max(0f, collider.Size.x) * 0.5f;
                        float halfHeight = Mathf.Max(0f, collider.Size.y) * 0.5f;

                        float angle = collider.Angle * Mathf.Deg2Rad;
                        float cos = Mathf.Abs(Mathf.Cos(angle));
                        float sin = Mathf.Abs(Mathf.Sin(angle));

                        extentX = halfWidth * cos + halfHeight * sin;
                        extentY = halfWidth * sin + halfHeight * cos;
                        break;
                    }

                case ColliderType.Capsule:
                    {
                        ColliderGeometry.GetCapsuleSegment(
                            Vector2.zero,
                            collider,
                            out Vector2 start,
                            out Vector2 end);

                        float radius = Mathf.Max(0f, collider.Radius);

                        extentX = Mathf.Max(
                            Mathf.Abs(start.x),
                            Mathf.Abs(end.x)) + radius;

                        extentY = Mathf.Max(
                            Mathf.Abs(start.y),
                            Mathf.Abs(end.y)) + radius;
                        break;
                    }

                case ColliderType.Sector:
                    {
                        // Conservative bounds for a sector.
                        float radius = Mathf.Max(0f, collider.Radius);

                        extentX = radius;
                        extentY = radius;
                        break;
                    }
            }
        }

        private bool IsCircleOverlappingCell(
            Vector3 worldPosition,
            float radius,
            int x,
            int y)
        {
            Vector3 gridPosition = transform.position;

            float minX = gridPosition.x +
                         (x - _cellsX * 0.5f) * _cellSize;

            float maxX = minX + _cellSize;

            float minY = gridPosition.y +
                         (y - _cellsY * 0.5f) * _cellSize;

            float maxY = minY + _cellSize;

            float closestX = Mathf.Clamp(worldPosition.x, minX, maxX);
            float closestY = Mathf.Clamp(worldPosition.y, minY, maxY);

            float dx = worldPosition.x - closestX;
            float dy = worldPosition.y - closestY;

            return dx * dx + dy * dy <= radius * radius;
        }

        private bool IsInside(int x, int y)
        {
            return x >= 0 && x < _cellsX &&
                   y >= 0 && y < _cellsY;
        }

        private int GetIndex(int x, int y)
        {
            return y * _cellsX + x;
        }

        private void EnsureDataSize()
        {
            int requiredSize = _cellsX * _cellsY;

            if (_blockedCells != null &&
                _blockedCells.Length == requiredSize)
            {
                return;
            }

            bool[] oldData = _blockedCells;
            _blockedCells = new bool[requiredSize];

            if (oldData == null)
                return;

            int copyLength = Mathf.Min(
                oldData.Length,
                _blockedCells.Length);

            Array.Copy(oldData, _blockedCells, copyLength);
        }
    }
}