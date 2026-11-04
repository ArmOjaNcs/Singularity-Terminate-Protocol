using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.CommonComponents
{
    public struct TraumaStatusComponent : IComponent
    {
        public List<TraumaInstance> Instances;
    }

    public struct TraumaInstance
    {
        public Entity SourceWeapon;
        public float RemainingDuration;
        public float MovementSlowPercent;
        public float AttackDamageReductionPercent;
    }
}
