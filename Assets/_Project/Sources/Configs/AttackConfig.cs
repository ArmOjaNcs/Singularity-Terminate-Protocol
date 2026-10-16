using ECS.AttackComponents;
using ECS.ProjectileComponents;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "AttackConfig", menuName = "Game/Attack/Attack Config")]
    public class AttackConfig : ScriptableObject
    {
        public int ProjectileCount;
        public int Pierce;
        public float AttackDuration;
        public float FireRate;
        public float Damage;
        public float AttackCooldown;
        public float DistanceFromSource;
        public float AttackRadius;
        public float TargetSearchRadius;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public AttackTargetType TargetType;
        public HitPolicyType HitPolicy;
        public float HitInterval;
    }
}