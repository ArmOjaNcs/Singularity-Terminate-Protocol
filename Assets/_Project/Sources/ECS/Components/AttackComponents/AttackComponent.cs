using ECS.ProjectileComponents;
using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
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
        public float AttackRadius;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public float HitInterval;
        public float RicochetChance;
        public float ShotSpreadAngle;

        public AttackTargetType TargetType;
        public HitPolicyType HitPolicy;
    }
}