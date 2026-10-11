using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.EffectComponents
{
    public struct EffectRequest : IComponent
    {
        public Entity Target;
        public DamageContext Context;
    }
}