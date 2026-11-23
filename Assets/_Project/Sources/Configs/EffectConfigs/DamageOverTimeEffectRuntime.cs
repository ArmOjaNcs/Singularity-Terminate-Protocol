using ECS.CommonComponents;
using EffectComponents;

namespace ECS.EffectComponents
{
    public sealed class DamageOverTimeEffectRuntime : IEffectRuntime
    {
        public EffectType Type { get; }
        public int MaxStacks;
        public float DamagePerStackPerSecond;
        public float Duration;

        public TwistEffectRuntime TwistOnDamage;

        public DamageOverTimeEffectRuntime(EffectType type)
        {
            Type = type;
        }
    }
}
