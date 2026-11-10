using ECS.ProjectileComponents;
using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    [System.Serializable]
    public struct AttackComponent : IComponent
    {
        public int ProjectileCount;
        public int ShotsInAttack;
        public int Pierce;
        public int MaxRicochets;

        public float AttackDuration;
        public float FireRate;
        public float Damage;
        public float AttackCooldown;
        public float CurrentCooldown;
        public float TargetSearchRadius;
        public float DistanceFromSource;
        public float ProjectileScale;
        public float RicochetTargetSearchRadius;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float HitInterval;
        public float RicochetChance;
        public bool AllowRicochetRepeatTargets;
        public float ZigZagAmplitude;
        public float ZigZagFrequency;
        public float ShotSpreadAngle;

        public AttackTargetType TargetType;
        public HitPolicyType HitPolicy;
        public AttackAnimationPolicyType AnimationPolicy;
        public ProjectileSequencePolicy ProjectileSequencePolicy;
    }
}
