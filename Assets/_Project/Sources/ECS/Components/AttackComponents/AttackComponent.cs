using ECS.ProjectileComponents;
using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    [System.Serializable]
    public struct AttackComponent : IComponent
    {
        public int CyclesPerAttack;
        public int ShotsPerCycle;
        public int MaxCyclesPerAttack;
        public int MaxShotsPerCycle;
        public int Pierce;
        public int MaxRicochets;

        public float CycleInterval;
        public float ShotInterval;
        public float Damage;
        public float AttackCooldown;
        public float CurrentCooldown;
        public float TargetSearchRadius;
        public float DistanceFromSource;
        public float ProjectileScale;
        public TrajectoryComponent Trajectory;
        public AttackPatternComponent Pattern;
        public bool HomingEnabled;
        public float RicochetTargetSearchRadius;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float HitInterval;
        public float RicochetChance;
        public bool AllowRicochetRepeatTargets;

        public AttackTargetType[] TargetTypes;
        public HitPolicyType HitPolicy;
        public AttackAnimationPolicyType AnimationPolicy;
        public ProjectileSequencePolicy ProjectileSequencePolicy;
    }
}
