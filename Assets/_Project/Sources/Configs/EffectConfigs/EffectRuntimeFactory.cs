using EffectComponents;
using EffectConfigs;

namespace ECS.EffectComponents
{
    public static class EffectRuntimeFactory
    {
        public static IEffectRuntime Create(EffectConfig config)
        {
            return config switch
            {
                BleedConfig bleedConfig => new BleedEffectRuntime
                {
                    MaxStacks = bleedConfig.MaxStacks,
                    DamagePerStackPerSecond =
                        bleedConfig.DamagePerStackPerSecond,
                    Duration = bleedConfig.Duration
                },

                BurnConfig burnConfig => new BurnEffectRuntime
                {
                    MaxStacks = burnConfig.MaxStacks,
                    DamagePerStackPerSecond =
                        burnConfig.DamagePerStackPerSecond,
                    Duration = burnConfig.Duration,
                    BasePanicInterval = burnConfig.BasePanicInterval,
                    MinPanicInterval = burnConfig.MinPanicInterval,
                    PanicDistance = burnConfig.PanicDistance,
                    PanicSpeedMultiplier = burnConfig.PanicSpeedMultiplier,
                    PanicBurstDuration = burnConfig.PanicBurstDuration
                },

                KnockbackEffectConfig knockbackConfig =>
                    new KnockbackEffectRuntime
                    {
                        Force = knockbackConfig.Force
                    },

                TraumaEffectConfig traumaConfig => new TraumaEffectRuntime
                {
                    Duration = traumaConfig.Duration,
                    MovementSlowPercent = traumaConfig.MovementSlowPercent,
                    AttackDamageReductionPercent = traumaConfig.AttackDamageReductionPercent
                },

                TwistEffectConfig twistConfig => new TwistEffectRuntime
                {
                    ShakeAmplitude = twistConfig.ShakeAmplitude,
                    ShakeFrequency = twistConfig.ShakeFrequency,
                    RefreshDuration = twistConfig.RefreshDuration
                },

                _ => null
            };
        }
    }
}
