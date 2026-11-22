using Gameplay.View;
using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(menuName = "Game/Projectile/Armageddon Projectile Config")]
    public sealed class ArmageddonProjectileConfig : GrenadeLauncherProjectileConfig
    {
        public AnimatedView SubmunitionPrefab;
        [Min(0.1f)] public float SubmunitionDistance = 1.6f;
        [Range(0f, 1f)] public float SubmunitionDamageMultiplier = 0.5f;
    }
}
