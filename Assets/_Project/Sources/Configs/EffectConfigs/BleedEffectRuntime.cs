using ECS.CommonComponents;
using EffectComponents;

namespace ECS.EffectComponents
{
    public class BleedEffectRuntime : IEffectRuntime
    {
        public EffectType Type => EffectType.Bleed;

        public int BleedStack;
        public float DamagePerSecond;
        public float Duration;
    }
}