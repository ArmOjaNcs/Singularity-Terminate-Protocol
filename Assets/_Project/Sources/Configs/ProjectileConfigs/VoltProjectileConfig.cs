using Gameplay.View;
using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(menuName = "Game/Projectile/Volt Projectile Config")]
    public sealed class VoltProjectileConfig : ProjectileConfig
    {
        public EmpWaveView WavePrefab;
        [Min(0.01f)] public float WaveRadius = 2f;
        [Min(0.01f)] public float WaveDuration = 0.4f;
    }
}
