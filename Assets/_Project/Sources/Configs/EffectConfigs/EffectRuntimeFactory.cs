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
                    BleedStack = bleedConfig.BleedStack,
                    DamagePerSecond = bleedConfig.DamagePerSecond,
                    Duration = bleedConfig.Duration
                },

                KnockbackEffectConfig knockbackConfig =>
                    new KnockbackEffectRuntime
                    {
                        Force = knockbackConfig.Force
                    },

                _ => null
            };
        }
    }
}