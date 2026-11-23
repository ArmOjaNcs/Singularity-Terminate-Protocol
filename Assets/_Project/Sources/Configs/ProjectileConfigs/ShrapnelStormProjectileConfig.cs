using Gameplay.View;
using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(menuName = "Game/Projectile/Shrapnel Storm Projectile Config")]
    public sealed class ShrapnelStormProjectileConfig : StraightProjectileConfig
    {
        public AnimatedView FragmentPrefab;
        public AnimatedView ImpactPrefab;
        [Min(0.01f)] public float ImpactDuration = 0.2f;
    }
}
