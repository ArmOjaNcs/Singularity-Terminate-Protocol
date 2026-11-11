using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(
        fileName = "GrenadeLauncherProjectileConfig",
        menuName = "Game/Projectile/Grenade Launcher Projectile Config")]
    public class GrenadeLauncherProjectileConfig : ProjectileConfig
    {
        [Min(0f)] public float ArcOffset = 0.75f;
        [Min(0.01f)] public float ExplosionDuration = 0.25f;
    }
}
