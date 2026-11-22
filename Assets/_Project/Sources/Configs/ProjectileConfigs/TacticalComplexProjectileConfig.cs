using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(
        fileName = "TacticalComplexProjectileConfig",
        menuName = "Game/Projectile/Tactical Complex Projectile Config")]
    public sealed class TacticalComplexProjectileConfig : ProjectileConfig
    {
        [Min(0f)] public float LaunchDistance = 1.8f;
        [Min(0.01f)] public float LaunchDuration = 0.2f;
        [Min(0f)] public float LockDelay = 0.75f;
        [Min(0.01f)] public float RetargetDelay = 0.25f;
        [Min(0.01f)] public float ExplosionDuration = 0.4f;
    }
}
