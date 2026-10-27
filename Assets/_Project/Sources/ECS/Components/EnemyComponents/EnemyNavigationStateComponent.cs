using Scellecs.Morpeh;

namespace ECS.EnemyComponents
{
    public struct EnemyNavigationStateComponent : IComponent
    {
        public float StuckTime;
        public float AvoidanceCheckTime;
        public int AvoidanceSide;
        public int PreferredSide;
    }
}
