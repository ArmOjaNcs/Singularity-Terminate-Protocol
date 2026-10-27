using ProjectileConfigs;
using UnityEngine;

namespace Gameplay.View
{
    public sealed class ProjectilePreview : MonoBehaviour
    {
        public ProjectileConfig Config;
        public Vector2 PreviewDirection = Vector2.right;
        public bool ShowCollider = true;
        public bool ShowTrajectory = true;

        private void OnDrawGizmos()
        {
            if (Config == null)
                return;

            Vector2 colliderOffset = Vector2.zero;
            if (Config is ArcProjectileConfig arcConfig)
                colliderOffset = (Vector2)arcConfig.ColliderOffset;

            if (ShowCollider)
                ColliderPreviewGizmos.Draw(Config.Collider, transform, Color.cyan, colliderOffset);

            if (ShowTrajectory && Config is ArcProjectileConfig arc)
                DrawArcTrajectory(arc);
        }

        public Vector3 GetWorldDirection()
        {
            Vector2 localDirection = PreviewDirection.sqrMagnitude > 0.0001f
                ? PreviewDirection.normalized
                : Vector2.right;
            Vector3 worldDirection = transform.TransformDirection(localDirection);
            worldDirection.z = 0f;
            return worldDirection.sqrMagnitude > 0.0001f
                ? worldDirection.normalized
                : Vector3.right;
        }

        private void DrawArcTrajectory(ArcProjectileConfig arc)
        {
            Color previousColor = Gizmos.color;
            float startAngle = arc.StartAngle;
            float endAngle = arc.EndAngle;
            Vector3 direction = GetWorldDirection();
            if (direction.x < 0f)
            {
                startAngle = -startAngle;
                endAngle = -endAngle;
            }

            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            float radius = Mathf.Max(0f, arc.TrajectoryRadius);
            const int segments = 48;
            Vector3 previous = GetTrajectoryPoint(
                direction,
                perpendicular,
                radius,
                startAngle);

            Gizmos.color = Color.magenta;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, i / (float)segments);
                Vector3 current = GetTrajectoryPoint(
                    direction,
                    perpendicular,
                    radius,
                    angle);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            Gizmos.DrawWireSphere(
                GetTrajectoryPoint(direction, perpendicular, radius, startAngle),
                0.06f);
            Gizmos.DrawWireSphere(
                GetTrajectoryPoint(direction, perpendicular, radius, endAngle),
                0.06f);
            Gizmos.color = previousColor;
        }

        private Vector3 GetTrajectoryPoint(
            Vector3 direction,
            Vector3 perpendicular,
            float radius,
            float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return transform.position +
                   direction * (Mathf.Cos(radians) * radius) +
                   perpendicular * (Mathf.Sin(radians) * radius);
        }
    }
}
