using ECS.ProjectileComponents;
using UnityEngine;

namespace Gameplay.View
{
    public sealed class ColliderPreview : MonoBehaviour
    {
        [Header("Collider")]
        public ColliderType Type = ColliderType.Circle;
        public float Radius = 0.5f;
        public float Length = 1f;
        public Vector2 Size = Vector2.one;
        [Range(0f, 360f)] public float SectorAngle = 180f;

        [Header("Drawing")]
        public Vector3 DrawPosition;
        public float DrawAngle;
        public Color Color = Color.cyan;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color;

            Matrix4x4 previousMatrix = Gizmos.matrix;

            Vector3 position = transform.TransformPoint(DrawPosition);

            Gizmos.matrix = Matrix4x4.TRS(
                position,
                transform.rotation * Quaternion.Euler(0f, 0f, DrawAngle),
                Vector3.one);

            switch (Type)
            {
                case ColliderType.Circle:
                    DrawCircle();
                    break;

                case ColliderType.Box:
                    DrawBox();
                    break;

                case ColliderType.Capsule:
                    DrawCapsule();
                    break;

                case ColliderType.Sector:
                    DrawSector();
                    break;
            }

            Gizmos.matrix = previousMatrix;
        }

        private void DrawCircle()
        {
            Gizmos.DrawWireSphere(
                Vector3.zero,
                Mathf.Max(0f, Radius));
        }

        private void DrawBox()
        {
            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(
                    Mathf.Max(0f, Size.x),
                    Mathf.Max(0f, Size.y),
                    0f));
        }

        private void DrawCapsule()
        {
            float radius = Mathf.Max(0f, Radius);
            float halfLength = Mathf.Max(0f, Length * 0.5f - radius);

            Vector3 top = Vector3.up * halfLength;
            Vector3 bottom = Vector3.down * halfLength;

            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);

            Gizmos.DrawLine(
                top + Vector3.left * radius,
                bottom + Vector3.left * radius);

            Gizmos.DrawLine(
                top + Vector3.right * radius,
                bottom + Vector3.right * radius);
        }

        private void DrawSector()
        {
            float radius = Mathf.Max(0f, Radius);
            float halfAngle = Mathf.Clamp(SectorAngle, 0f, 360f) * 0.5f;

            const int segments = 48;

            Vector3 previousPoint = GetSectorPoint(-halfAngle, radius);

            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    i / (float)segments);

                Vector3 currentPoint = GetSectorPoint(angle, radius);

                Gizmos.DrawLine(previousPoint, currentPoint);

                previousPoint = currentPoint;
            }

            Gizmos.DrawLine(
                Vector3.zero,
                GetSectorPoint(-halfAngle, radius));

            Gizmos.DrawLine(
                Vector3.zero,
                GetSectorPoint(halfAngle, radius));
        }

        private Vector3 GetSectorPoint(float angle, float radius)
        {
            float radians = angle * Mathf.Deg2Rad;

            return new Vector3(
                Mathf.Cos(radians) * radius,
                Mathf.Sin(radians) * radius,
                0f);
        }
    }
}