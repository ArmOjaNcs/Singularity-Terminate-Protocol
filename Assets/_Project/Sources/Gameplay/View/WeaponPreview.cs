using UnityEngine;
using WeaponConfigs;

namespace Gameplay.View
{
    public sealed class WeaponPreview : MonoBehaviour
    {
        public WeaponConfig Config;
        public Vector2 PreviewDirection = Vector2.right;
        public bool ShowTargetSearchRadius = true;
        public bool ShowDistanceFromSource = true;

        private void OnDrawGizmos()
        {
            if (Config == null || Config.AttackConfig == null)
                return;

            Vector3 direction = GetWorldDirection();

            if (ShowTargetSearchRadius)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(
                    transform.position,
                    Mathf.Max(0f, Config.AttackConfig.TargetSearchRadius));
            }

            if (ShowDistanceFromSource)
            {
                float distance = Config.AttackConfig.DistanceFromSource;
                Vector3 end = transform.position + direction * distance;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, end);
                Gizmos.DrawWireSphere(end, 0.08f);
            }
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
    }
}
