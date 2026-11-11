using ECS.CommonComponents;
using EffectComponents;

namespace ECS.EffectComponents
{
    public class ShockEffectRuntime : IEffectRuntime
    {
        public EffectType Type => EffectType.Shock;
        public int MaxStacks;
        public float DamagePerStackPerSecond;
        public float Duration;
        public TwistEffectRuntime TwistOnDamage;
    }
}
