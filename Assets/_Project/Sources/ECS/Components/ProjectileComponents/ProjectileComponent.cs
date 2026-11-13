using ECS.AttackComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct ProjectileComponent : IComponent
    {
        public Entity Weapon;
        public WeaponType WeaponType;

        public float Speed;
        public float Lifetime;
        public float CurrentLifetime;
        public float HitSuppressionUntilTime;
        public float Damage;
        public int Pierce;
        public int RicochetsCount;
        public int RicochetLeg;
        public int MaxRicochets;
        public float HitInterval;
        public float RicochetChance;
        public bool AllowRicochetRepeatTargets;
        public Entity TemporarilyIgnoredTarget;
        public float TargetSearchRadius;
        public ProjectileSequencePolicy SequencePolicy;
        public int SequenceId;
        public int SwingIndex;

        public Vector3 Direction;
        public AttackTargetType[] TargetTypes;
        public HitPolicyType HitPolicy;

        public bool CleanupRequested;
        public bool RicochetPending;
        public bool CollisionEnabled;
    }
}
