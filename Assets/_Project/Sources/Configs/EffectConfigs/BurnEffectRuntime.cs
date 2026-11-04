using ECS.CommonComponents;
using EffectComponents;

namespace ECS.EffectComponents
{
    public class BurnEffectRuntime : IEffectRuntime
    {
        public EffectType Type => EffectType.Burn;

        public int MaxStacks;
        public float DamagePerStackPerSecond;
        public float Duration;
        public float BasePanicInterval;
        public float MinPanicInterval;
        public float PanicDistance;
        public float PanicSpeedMultiplier;
        public float PanicBurstDuration;
    }
}
