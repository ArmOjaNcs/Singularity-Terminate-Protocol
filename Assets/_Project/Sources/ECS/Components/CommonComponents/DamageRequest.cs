using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.DamageComponents
{
    public struct DamageRequest : IComponent
    {
        public Entity Target;
        public float Damage;
        public DamageContext Context;
    }
}