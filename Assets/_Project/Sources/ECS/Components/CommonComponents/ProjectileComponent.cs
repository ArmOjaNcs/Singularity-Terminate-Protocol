using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct ProjectileComponent : IComponent
    {
        public Entity Owner;
        public float Lifetime;
        public float CurrentLifetime;
    }
}