using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(
        fileName = "RocketLauncherProjectileConfig",
        menuName = "Game/Projectile/Rocket Launcher Projectile Config")]
    public sealed class RocketLauncherProjectileConfig : ProjectileConfig
    {
        [Min(0.01f)] public float ExplosionDuration = 0.3f;
        [Min(0f)] public float HomingTurnSpeed = 480f;
    }
}
