using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.CommonComponents
{
    public struct BleedStatusComponent : IComponent
    {
        public List<BleedInstance> Instances;
    }

    public struct BleedInstance
    {
        public Entity SourceWeapon;
        public DamageContext SourceContext;
        public int Stacks;
        public int MaxStacks;
        public float DamagePerStackPerSecond;
        public float RemainingDuration;
        public float TickElapsed;
        public float PendingDamage;
    }
}
