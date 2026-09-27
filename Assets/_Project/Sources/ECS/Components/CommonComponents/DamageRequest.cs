using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct DamageRequest : IComponent
    {
        public Entity Source;
        public Entity Target;
        public float Damage;
    }
}