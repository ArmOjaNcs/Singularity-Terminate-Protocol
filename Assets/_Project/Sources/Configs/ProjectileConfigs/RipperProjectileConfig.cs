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
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0.866f),
            new Vector2(-0.5f, 0.866f),
            new Vector2(-1f, 0f),
            new Vector2(-0.5f, -0.866f),
            new Vector2(0.5f, -0.866f)
        };
    }
}
