using Scellecs.Morpeh;

namespace ECS.EnemyComponents
{
    public struct EnemyMovementPolicyComponent : IComponent
    {
        public MovePolicyType MovePolicy;
        public AttackPolicyType AttackPolicy;
        public float PreferredAttackDistance;
    }
}
