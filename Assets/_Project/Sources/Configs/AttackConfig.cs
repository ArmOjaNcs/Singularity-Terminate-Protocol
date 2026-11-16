using ECS.AttackComponents;
using ECS.ProjectileComponents;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "AttackConfig", menuName = "Game/Attack/Attack Config")]
    public class AttackConfig : ScriptableObject
    {
        public int CyclesPerAttack;
        public int ShotsPerCycle;
        [Min(1)] public int MaxCyclesPerAttack = 12;
        [Min(1)] public int MaxShotsPerCycle = 12;
        public int Pierce;
        public int MaxRicochets;

        public float CycleInterval;
        [Min(0f)] public float ShotInterval;
        public float Damage;
        public float AttackCooldown;
        public float DistanceFromSource;
        public float ProjectileScale = 1f;
        public TrajectoryConfig Trajectory;
        public AttackPatternConfig Pattern;
        public bool HomingEnabled;
        public float RicochetTargetSearchRadius;
        public float TargetSearchRadius;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float RicochetChance;
        public bool AllowRicochetRepeatTargets;
        public float HitInterval;

        public AttackTargetType[] TargetType;
        public HitPolicyType HitPolicy;
        public AttackAnimationPolicyType AnimationPolicy;
        public ProjectileSequencePolicy ProjectileSequencePolicy;
    }
}
