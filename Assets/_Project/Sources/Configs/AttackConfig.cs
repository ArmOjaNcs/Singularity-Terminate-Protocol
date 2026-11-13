using ECS.AttackComponents;
using ECS.ProjectileComponents;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "AttackConfig", menuName = "Game/Attack/Attack Config")]
    public class AttackConfig : ScriptableObject
    {
        public int ProjectileCount;
        public int ShotsInAttack;
        public int Pierce;
        public int MaxRicochets;

        public float AttackDuration;
        public float FireRate;
        public float Damage;
        public float AttackCooldown;
        public float DistanceFromSource;
        public float ProjectileScale = 1f;
        [Tooltip("Fixed forward distance from launch to landing for arcing projectiles.")]
        [Min(0f)] public float ProjectileFlightDistance;
        [Tooltip("Targets are selected within this radius of the fixed landing range.")]
        [Min(0f)] public float ProjectileFlightDistanceTolerance;
        public bool HomingEnabled;
        public float RicochetTargetSearchRadius;
        public float TargetSearchRadius;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float RicochetChance;
        public bool AllowRicochetRepeatTargets;
        [Min(0f)] public float ZigZagAmplitude = 0.3f;
        [Min(0f)] public float ZigZagFrequency = 8f;
        public float ShotSpreadAngle;
        public float HitInterval;

        public AttackTargetType[] TargetType;
        public HitPolicyType HitPolicy;
        public AttackAnimationPolicyType AnimationPolicy;
        public ProjectileSequencePolicy ProjectileSequencePolicy;
    }
}
