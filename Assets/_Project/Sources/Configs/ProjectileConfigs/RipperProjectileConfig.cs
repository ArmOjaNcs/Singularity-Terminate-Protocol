using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(
        fileName = "RipperProjectileConfig",
        menuName = "Game/Projectile/Ripper Projectile Config")]
    public class RipperProjectileConfig : ProjectileConfig
    {
        public Vector2[] AttackDirections =
        {
            new Vector2(0.7071f, 0.7071f),
            new Vector2(-0.7071f, 0.7071f),
            new Vector2(-0.7071f, -0.7071f),
            new Vector2(0.7071f, -0.7071f)
        };
    }
}
