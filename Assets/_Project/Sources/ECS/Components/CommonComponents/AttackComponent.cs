using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct AttackComponent : IComponent
    {
        public float Damage;
        public float AttackRadius;
        public float AttackCooldown;
        public float CurrentCooldown;
        public AttackTargetType TargetType;
    }
}