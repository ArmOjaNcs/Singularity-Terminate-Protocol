using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(fileName = "ArcProjectileConfig", menuName = "Game/Projectile/Arc Projectile Config")]
    public class ArcProjectileConfig : ProjectileConfig
    {
        public Vector3 ColliderOffset;
        public float TrajectoryRadius;
        public float StartAngle;
        public float EndAngle;
    }
}