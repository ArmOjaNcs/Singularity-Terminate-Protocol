using UnityEngine;

namespace ECS.ProjectileSystems
{
    internal static class CardinalSectors
    {
        public const int Count = 4;

        public static int FromDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return 0;

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                return direction.x < 0f ? 2 : 0;

            return direction.y < 0f ? 3 : 1;
        }

        public static int FindAvailable(int preferredSector, int usedMask)
        {
            for (int offset = 0; offset < Count; offset++)
            {
                int sector = (preferredSector + offset) % Count;
                if ((usedMask & (1 << sector)) == 0)
                    return sector;
            }

            return -1;
        }

        public static Vector3 ToDirection(int sector)
        {
            return sector switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                2 => Vector3.left,
                3 => Vector3.down,
                _ => Vector3.right
            };
        }
    }
}
