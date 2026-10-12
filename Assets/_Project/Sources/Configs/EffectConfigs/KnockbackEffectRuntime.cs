using ECS.CommonComponents;

namespace EffectComponents
{
    public class KnockbackEffectRuntime : IEffectRuntime
    {
        public EffectType Type => EffectType.Knockback;

        public float Force;
    }
}