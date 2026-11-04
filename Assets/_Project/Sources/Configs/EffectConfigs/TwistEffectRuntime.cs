using ECS.CommonComponents;

namespace EffectComponents
{
    public class TwistEffectRuntime : IEffectRuntime
    {
        public EffectType Type => EffectType.Twist;

        public float ShakeAmplitude;
        public float ShakeFrequency;
        public float RefreshDuration;
    }
}
