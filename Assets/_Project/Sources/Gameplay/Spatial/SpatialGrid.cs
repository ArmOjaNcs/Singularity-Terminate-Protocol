using System.Collections.Generic;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Spatial
{
    public sealed class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly float _inverseCellSize;

        private readonly Dictionary<int, List<Entity>> _cells =
            new Dictionary<int, List<Entity>>();

        public SpatialGrid(float cellSize)
        {
            if (cellSize <= 0f)
                throw new System.ArgumentOutOfRangeException(
                    nameof(cellSize),
                    "Cell size must be greater than zero.");

            _cellSize = cellSize;
            _inverseCellSize = 1f / cellSize;
        }

        public void Clear()
        {
            foreach (List<Entity> entities in _cells.Values)
                entities.Clear();
        }

        public void Add(Entity entity, Vector3 position)
        {
            int key = GetCellKey(position);

            if (!_cells.TryGetValue(key, out List<Entity> entities))
            {
                entities = new List<Entity>();
                _cells.Add(key, entities);
            }

            entities.Add(entity);
        }

        public void Query(
            Vector3 center,
            float radius,
            List<Entity> results)
        {
            results.Clear();

            if (radius < 0f)
                return;

            int minX = GetCellCoordinate(center.x - radius);
            int maxX = GetCellCoordinate(center.x + radius);

            int minZ = GetCellCoordinate(center.z - radius);
            int maxZ = GetCellCoordinate(center.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    int key = GetCellKey(x, z);

                    if (!_cells.TryGetValue(
                            key,
                            out List<Entity> entities))
                        continue;

                    for (int i = 0; i < entities.Count; i++)
                        results.Add(entities[i]);
                }
            }
        }

        private int GetCellCoordinate(float position)
        {
            return Mathf.FloorToInt(position * _inverseCellSize);
        }

        private int GetCellKey(Vector3 position)
        {
            int x = GetCellCoordinate(position.x);
            int z = GetCellCoordinate(position.z);

            return GetCellKey(x, z);
        }

        private int GetCellKey(int x, int z)
        {
            unchecked
            {
                return (x * 73856093) ^ (z * 19349663);
            }
        }
    }
}