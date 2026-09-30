using EnemyConfigs;
using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    public struct AttackComponent : IComponent
    {
        public bool IsAttacking;
        public float Damage;
        public float AttackRadius;
        public float AttackCooldown;
        public float CurrentCooldown;
        public AttackTargetType TargetType;
        public string SourceId;
        public ProjectileConfig ProjectileConfig;
    }
}