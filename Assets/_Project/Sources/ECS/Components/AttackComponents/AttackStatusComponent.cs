using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    public struct AttackStatusComponent : IComponent
    {
        public bool IsAttacking;
        public bool AnimationFinished;
        public bool AttackPerformed;
        public bool CreationFinished;
    }
}