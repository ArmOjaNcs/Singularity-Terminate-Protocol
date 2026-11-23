using System;
using ECS.AttackComponents;
using Scellecs.Morpeh;
using UpgradeConfigs;

namespace ECS.WeaponSystems
{
    internal sealed class WeaponStatUpgrades
    {
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<SwordAttackComponent> _swordStash;

        public WeaponStatUpgrades(World world)
        {
            _attackStash = world.GetStash<AttackComponent>();
            _swordStash = world.GetStash<SwordAttackComponent>();
        }

        public void Apply(Entity weaponEntity, WeaponStatUpgradeConfig config)
        {
            ref AttackComponent attack = ref _attackStash.Get(weaponEntity);
            int amount = config.Amount;

            switch (config.Stat)
            {
                case WeaponStats.CyclesPerAttack:
                    attack.CyclesPerAttack = Math.Min(
                        attack.MaxCyclesPerAttack,
                        attack.CyclesPerAttack + amount);
                    break;
                case WeaponStats.ShotsPerCycle:
                    attack.ShotsPerCycle = Math.Min(
                        attack.MaxShotsPerCycle,
                        attack.ShotsPerCycle + amount);
                    break;
                case WeaponStats.Pierce:
                    attack.Pierce += amount;
                    break;
                case WeaponStats.AttackDuration:
                    attack.Trajectory.Duration += PercentDelta(attack.Trajectory.Duration, amount);
                    break;
                case WeaponStats.CycleInterval:
                    attack.CycleInterval += PercentDelta(attack.CycleInterval, amount);
                    break;
                case WeaponStats.Damage:
                    attack.Damage += amount;
                    break;
                case WeaponStats.AttackCooldown:
                    attack.AttackCooldown -= PercentDelta(attack.AttackCooldown, amount);
                    break;
                case WeaponStats.DistanceFromSource:
                    attack.DistanceFromSource += PercentDelta(attack.DistanceFromSource, amount);
                    break;
                case WeaponStats.RicochetTargetSearchRadius:
                    attack.RicochetTargetSearchRadius += PercentDelta(attack.RicochetTargetSearchRadius, amount);
                    break;
                case WeaponStats.TargetSearchRadius:
                    attack.TargetSearchRadius += PercentDelta(attack.TargetSearchRadius, amount);
                    break;
                case WeaponStats.ProjectileSpeed:
                    attack.ProjectileSpeed += PercentDelta(attack.ProjectileSpeed, amount);
                    break;
                case WeaponStats.ProjectileLifetime:
                    attack.ProjectileLifetime += PercentDelta(attack.ProjectileLifetime, amount);
                    break;
                case WeaponStats.ProjectileScale:
                {
                    float previousScale = attack.ProjectileScale;
                    attack.ProjectileScale += PercentDelta(attack.ProjectileScale, amount);
                    if (attack.ScaleAttackGeometryWithProjectile &&
                        previousScale > 0f)
                    {
                        float scaleRatio = attack.ProjectileScale / previousScale;
                        attack.DistanceFromSource *= scaleRatio;
                        attack.Trajectory.FlightDistance *= scaleRatio;
                        attack.Trajectory.ZigZagAmplitude *= scaleRatio;

                        if (_swordStash.Has(weaponEntity))
                        {
                            ref SwordAttackComponent sword = ref _swordStash.Get(weaponEntity);
                            sword.ThrustDistance *= scaleRatio;
                            sword.ZigZagWidth *= scaleRatio;
                        }
                    }

                    break;
                }

                case WeaponStats.Homing:
                    attack.HomingEnabled = true;
                    break;
                default:
                    break;
            }
        }

        private static float PercentDelta(float value, int amount)
        {
            return (float)Math.Round(value * (amount / 100f), 2);
        }
    }
}
