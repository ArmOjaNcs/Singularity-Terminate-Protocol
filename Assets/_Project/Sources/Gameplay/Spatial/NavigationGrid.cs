using System;
using UnityEngine;

namespace Gameplay.Navigation
{
    public sealed class NavigationGrid : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;
        [SerializeField, Min(1)] private int _cellsX = 50;
        [SerializeField, Min(1)] private int _cellsZ = 50;

        [Header("Blocked Cells")]
        [SerializeField] private bool[] _blockedCells;

        public float CellSize => _cellSize;
        public int CellsX => _cellsX;
        public int CellsZ => _cellsZ;

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
            if (!TryGetCell(worldPosition, out int x, out int z))
                return false;

            return IsWalkable(x, z);
        }

        public bool IsWalkable(Vector3 worldPosition, float radius)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(worldPosition);

            float localRadius = GetLocalRadius(radius);

            float halfWidth =
                _cellsX * _cellSize * 0.5f;

            float halfHeight =
                _cellsZ * _cellSize * 0.5f;

            // ѕровер€ем выход окружности за границы Grid.
            if (localPosition.x - localRadius < -halfWidth ||
                localPosition.x + localRadius > halfWidth ||
                localPosition.z - localRadius < -halfHeight ||
                localPosition.z + localRadius > halfHeight)
            {
                return false;
            }

            if (!TryGetCell(
                worldPosition,
                out int centerX,
                out int centerZ))
            {
                return false;
            }

            int cellRadius = Mathf.CeilToInt(
                localRadius / _cellSize
            );

            for (int x = centerX - cellRadius;
                 x <= centerX + cellRadius;
                 x++)
            {
                for (int z = centerZ - cellRadius;
                     z <= centerZ + cellRadius;
                     z++)
                {
                    if (!IsInside(x, z))
                        return false;

                    if (!IsWalkable(x, z))
                    {
                        if (IsCircleOverlappingCell(
                            worldPosition,
                            radius,
                            x,
                            z))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool IsWalkable(int x, int z)
        {
            if (!IsInside(x, z))
                return false;

            return !_blockedCells[GetIndex(x, z)];
        }

        public bool IsBlocked(int x, int z)
        {
            if (!IsInside(x, z))
                return true;

            return _blockedCells[GetIndex(x, z)];
        }

        public void SetBlocked(
            int x,
            int z,
            bool blocked)
        {
            if (!IsInside(x, z))
                return;

            _blockedCells[GetIndex(x, z)] = blocked;
        }

        public bool TryGetCell(
            Vector3 worldPosition,
            out int x,
            out int z)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(worldPosition);

            x = Mathf.FloorToInt(
                localPosition.x / _cellSize +
                _cellsX * 0.5f);

            z = Mathf.FloorToInt(
                localPosition.z / _cellSize +
                _cellsZ * 0.5f);

            return IsInside(x, z);
        }

        public Vector3 GetCellCenter(
            int x,
            int z)
        {
            if (!IsInside(x, z))
                throw new ArgumentOutOfRangeException();

            Vector3 localPosition = new Vector3(
                (x + 0.5f - _cellsX * 0.5f) * _cellSize,
                0f,
                (z + 0.5f - _cellsZ * 0.5f) * _cellSize);

            return transform.TransformPoint(localPosition);
        }

        public Vector3 GetGridSize()
        {
            return new Vector3(
                _cellsX * _cellSize,
                0f,
                _cellsZ * _cellSize);
        }

        private bool IsCircleOverlappingCell(
                Vector3 worldPosition,
                float worldRadius,
                int x,
                int z)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(worldPosition);

            float localRadius =
                GetLocalRadius(worldRadius);

            float minX =
                (x - _cellsX * 0.5f) * _cellSize;

            float maxX =
                minX + _cellSize;

            float minZ =
                (z - _cellsZ * 0.5f) * _cellSize;

            float maxZ =
                minZ + _cellSize;

            float closestX =
                Mathf.Clamp(localPosition.x, minX, maxX);

            float closestZ =
                Mathf.Clamp(localPosition.z, minZ, maxZ);

            float dx =
                localPosition.x - closestX;

            float dz =
                localPosition.z - closestZ;

            return dx * dx + dz * dz <=
                   localRadius * localRadius;
        }

        private float GetLocalRadius(float worldRadius)
        {
            Vector3 scale =
                transform.lossyScale;

            float scaleX =
                Mathf.Abs(scale.x);

            float scaleZ =
                Mathf.Abs(scale.z);

            float minimumScale =
                Mathf.Min(scaleX, scaleZ);

            if (minimumScale <= Mathf.Epsilon)
                return 0f;

            return worldRadius / minimumScale;
        }

        private bool IsInside(int x, int z)
        {
            return x >= 0 &&
                   x < _cellsX &&
                   z >= 0 &&
                   z < _cellsZ;
        }

        private int GetIndex(int x, int z)
        {
            return z * _cellsX + x;
        }

        private void EnsureDataSize()
        {
            int requiredSize =
                _cellsX * _cellsZ;

            if (_blockedCells != null && _blockedCells.Length == requiredSize)
                return;

            bool[] oldData =
                _blockedCells;

            _blockedCells =
                new bool[requiredSize];

            if (oldData == null)
                return;

            int copyLength =
                Mathf.Min(
                    oldData.Length,
                    _blockedCells.Length);

            Array.Copy(
                oldData,
                _blockedCells,
                copyLength);
        }
    }
}