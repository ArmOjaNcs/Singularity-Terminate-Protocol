using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.DamageComponents
{
    public struct DamageStatisticComponent : IComponent
    {
        public Entity Target;
        public DamageContext Context;
        public float Damage;
        public float BlockedDamage;
    }
}