using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.CommonComponents
{
    public struct BurnStatusComponent : IComponent
    {
        public List<BurnInstance> Instances;
    }

    public struct BurnInstance
    {
        public Entity SourceWeapon;
        public DamageContext SourceContext;
        public int Stacks;
        public int MaxStacks;
        public float DamagePerStackPerSecond;
        public float RemainingDuration;
        public float TickElapsed;
        public float PendingDamage;
        public float BasePanicInterval;
        public float MinPanicInterval;
        public float PanicCooldownRemaining;
        public float PanicDistance;
        public float PanicSpeedMultiplier;
        public float PanicBurstDuration;
    }
}
