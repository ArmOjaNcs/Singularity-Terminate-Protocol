using Scellecs.Morpeh;
using ECS.CommonComponents;

namespace ECS.ProjectileComponents
{
    public struct AreaExpansionComponent : IComponent
    {
        public ColliderComponent BaseCollider;
        public float Duration;
        public float Elapsed;
        public bool ReachedMaximum;
    }
}
