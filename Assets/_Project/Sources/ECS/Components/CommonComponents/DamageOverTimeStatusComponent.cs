using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.CommonComponents
{
    public struct DamageOverTimeStatusComponent : IComponent
    {
        public List<DamageOverTimeInstance> Instances;
    }

    public struct DamageOverTimeInstance
    {
        public Entity SourceWeapon;
        public DamageContext SourceContext;
        public EffectType EffectType;
        public int Stacks;
        public int MaxStacks;
        public float DamagePerStackPerSecond;
        public float RemainingDuration;
        public float TickElapsed;
        public float PendingDamage;
        public bool ApplyTwistOnDamage;
        public float TwistShakeAmplitude;
        public float TwistShakeFrequency;
        public float TwistDuration;
    }
}
