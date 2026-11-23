using ECS.CommonComponents;
using EffectComponents;
using EffectConfigs;
using UnityEngine;
using WeaponConfigs;

namespace ECS.EffectComponents
{
    public static class EffectRuntimeFactory
    {
        public static EffectCollection CreateCollection(WeaponConfig weapon)
        {
            var effects = new EffectCollection();
            if (weapon.Effects == null)
                return effects;

            foreach (EffectConfig config in weapon.Effects)
            {
                if (config == null)
                    continue;

                IEffectRuntime effect = Create(config);
                if (effect != null)
                    effects.Add(effect);
                else
                    Debug.LogWarning(
                        $"Weapon {weapon.Type} has an unsupported effect config '{config.name}'.", config);
            }

            return effects;
        }

        private static IEffectRuntime Create(EffectConfig config)
        {
            return config switch
            {
                BleedConfig bleedConfig => new DamageOverTimeEffectRuntime(EffectType.Bleed)
                {
                    MaxStacks = bleedConfig.MaxStacks,
                    DamagePerStackPerSecond =
                        bleedConfig.DamagePerStackPerSecond,
                    Duration = bleedConfig.Duration
                },

                ShockEffectConfig shockConfig => new DamageOverTimeEffectRuntime(EffectType.Shock)
                {
                    MaxStacks = shockConfig.MaxStacks,
                    DamagePerStackPerSecond = shockConfig.DamagePerStackPerSecond,
                    Duration = shockConfig.Duration,
                    TwistOnDamage = CreateTwist(shockConfig.TwistOnDamage)
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

                TwistEffectConfig twistConfig => CreateTwist(twistConfig),

                _ => null
            };
        }

        private static TwistEffectRuntime CreateTwist(TwistEffectConfig config)
        {
            if (config == null)
                return null;

            return new TwistEffectRuntime
            {
                ShakeAmplitude = config.ShakeAmplitude,
                ShakeFrequency = config.ShakeFrequency,
                RefreshDuration = config.RefreshDuration
            };
        }
    }
}
