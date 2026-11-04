using ECS.CommonComponents;
using EffectComponents;

namespace EffectComponents
{
    public class TraumaEffectRuntime : IEffectRuntime
    {
        public EffectType Type => EffectType.Trauma;

        public float Duration;
        public float MovementSlowPercent;
        public float AttackDamageReductionPercent;
    }
}
