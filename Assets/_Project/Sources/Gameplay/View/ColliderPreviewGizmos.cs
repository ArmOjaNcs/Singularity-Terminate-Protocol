using Configs;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using UnityEngine;

namespace Gameplay.View
{
    internal static class ColliderPreviewGizmos
    {
        public static void Draw(
            ColliderConfig collider,
            Transform owner,
            Color color,
            Vector2 additionalCenterOffset = default)
        {
            if (collider == null || owner == null)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Vector3 localCenter = collider.Center + additionalCenterOffset;

            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(
                owner.TransformPoint(localCenter),
                owner.rotation,
                Vector3.one);

            switch (collider.Type)
            {
                case ColliderType.Circle:
                    Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(0f, collider.Radius));
                    break;

                case ColliderType.Box:
                    Gizmos.DrawWireCube(
                        Vector3.zero,
                        new Vector3(
                            Mathf.Max(0f, collider.Size.x),
                            Mathf.Max(0f, collider.Size.y),
                            0f));
                    break;

                case ColliderType.Capsule:
                    DrawCapsule(collider);
                    break;

                case ColliderType.Sector:
                    DrawSector(collider);
                    break;
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private static void DrawCapsule(ColliderConfig collider)
        {
            float radius = Mathf.Max(0f, collider.Radius);
            float halfLength = Mathf.Max(0f, collider.Length * 0.5f - radius);
            Vector3 axis = collider.CapsuleOrientation == CapsuleOrientation.Y
                ? Vector3.up
                : Vector3.right;
            Vector3 perpendicular = new Vector3(-axis.y, axis.x, 0f);
            Vector3 first = axis * halfLength;
            Vector3 second = -axis * halfLength;

            Gizmos.DrawWireSphere(first, radius);
            Gizmos.DrawWireSphere(second, radius);
            Gizmos.DrawLine(first + perpendicular * radius, second + perpendicular * radius);
            Gizmos.DrawLine(first - perpendicular * radius, second - perpendicular * radius);
        }

        private static void DrawSector(ColliderConfig collider)
        {
            float radius = Mathf.Max(0f, collider.Radius);
            float halfAngle = Mathf.Clamp(collider.Size.x, 0f, 360f) * 0.5f;
            const int segments = 48;

            Vector3 previous = GetSectorPoint(-halfAngle, radius);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
                Vector3 current = GetSectorPoint(angle, radius);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            Gizmos.DrawLine(Vector3.zero, GetSectorPoint(-halfAngle, radius));
            Gizmos.DrawLine(Vector3.zero, GetSectorPoint(halfAngle, radius));
        }

        private static Vector3 GetSectorPoint(float angle, float radius)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);
        }
    }
}