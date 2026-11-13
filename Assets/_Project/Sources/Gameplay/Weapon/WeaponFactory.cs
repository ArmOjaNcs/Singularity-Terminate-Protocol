using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EffectComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using EffectComponents;
using Scellecs.Morpeh;
using UnityEngine;
using WeaponConfigs;

namespace ECS.WeaponSystems
{
    public sealed class WeaponFactory
    {
        private readonly World _world;

        private readonly Stash<WeaponTag> _weaponTagStash;
        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;
        private readonly Stash<ProjectileCreationComponent> _projectileCreationStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<WeaponEffectsComponent> _weaponEffectsStash;

        public WeaponFactory(World world)
        {
            _world = world;

            _weaponTagStash = world.GetStash<WeaponTag>();
            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
            _projectileCreationStash = world.GetStash<ProjectileCreationComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _weaponEffectsStash = world.GetStash<WeaponEffectsComponent>();
        }

        public bool Create(WeaponConfig config, Entity owner)
        {
            if (config == null)
            {
                Debug.LogError("Cannot create a weapon because its WeaponConfig is missing.");
                return false;
            }

            if (config.AttackConfig == null)
            {
                Debug.LogError(
                    $"Cannot create weapon {config.Type}: its AttackConfig is missing.");
                return false;
            }

            Entity weapon = _world.CreateEntity();

            _weaponTagStash.Set(weapon, new WeaponTag());

            _weaponStash.Set(weapon, new WeaponComponent
            {
                Owner = owner,
                Level = 1,
                Type = config.Type
            });

            _attackStash.Set(weapon, new AttackComponent
            {
                CyclesPerAttack = Mathf.Clamp(
                    config.AttackConfig.CyclesPerAttack,
                    0,
                    Mathf.Max(1, config.AttackConfig.MaxCyclesPerAttack)),
                ShotsPerCycle = Mathf.Clamp(
                    config.AttackConfig.ShotsPerCycle,
                    0,
                    Mathf.Max(1, config.AttackConfig.MaxShotsPerCycle)),
                MaxCyclesPerAttack = Mathf.Max(1, config.AttackConfig.MaxCyclesPerAttack),
                MaxShotsPerCycle = Mathf.Max(1, config.AttackConfig.MaxShotsPerCycle),
                Pierce = config.AttackConfig.Pierce,
                MaxRicochets = config.AttackConfig.MaxRicochets,

                CycleInterval = config.AttackConfig.CycleInterval,
                ShotInterval = config.AttackConfig.ShotInterval,
                Damage = config.AttackConfig.Damage,
                AttackCooldown = config.AttackConfig.AttackCooldown,
                CurrentCooldown = config.AttackConfig.AttackCooldown,
                TargetSearchRadius = config.AttackConfig.TargetSearchRadius,
                DistanceFromSource = config.AttackConfig.DistanceFromSource,
                ProjectileScale = config.AttackConfig.ProjectileScale,
                Trajectory = new TrajectoryComponent
                {
                    Duration = config.AttackConfig.Trajectory.Duration,
                    FlightDistance = config.AttackConfig.Trajectory.FlightDistance,
                    FlightDistanceTolerance = config.AttackConfig.Trajectory.FlightDistanceTolerance,
                    ZigZagAmplitude = config.AttackConfig.Trajectory.ZigZagAmplitude,
                    ZigZagFrequency = config.AttackConfig.Trajectory.ZigZagFrequency,
                    ShakeAmplitude = config.AttackConfig.Trajectory.ShakeAmplitude,
                    ShakeFrequency = config.AttackConfig.Trajectory.ShakeFrequency
                },
                Pattern = new AttackPatternComponent
                {
                    SpreadAngle = config.AttackConfig.Pattern.SpreadAngle,
                    RandomDirectionFallback = config.AttackConfig.Pattern.RandomDirectionFallback,
                    SelectTargetPerCycle = config.AttackConfig.Pattern.SelectTargetPerCycle
                },
                HomingEnabled = config.AttackConfig.HomingEnabled,
                RicochetTargetSearchRadius = config.AttackConfig.RicochetTargetSearchRadius,
                ProjectileSpeed = config.AttackConfig.ProjectileSpeed,
                ProjectileLifetime = config.AttackConfig.ProjectileLifetime,
                RicochetChance = config.AttackConfig.RicochetChance,
                AllowRicochetRepeatTargets = config.AttackConfig.AllowRicochetRepeatTargets,
                TargetTypes = config.AttackConfig.TargetType,
                HitPolicy = config.AttackConfig.HitPolicy,
                HitInterval = config.AttackConfig.HitInterval,
                AnimationPolicy = config.AttackConfig.AnimationPolicy,
                ProjectileSequencePolicy =
                    config.AttackConfig.ProjectileSequencePolicy
            });

            _attackStatusStash.Set(weapon, new AttackStatusComponent
            {
                IsAttacking = false,
                AnimationFinished = true,
                AttackPerformed = false,
                CreationFinished = true
            });

            _projectileCreationStash.Set(weapon, new ProjectileCreationComponent
            {
                ElapsedTime = 0f,
                RemainingCount = 0,
                IsInitialized = false
            });

            _world.GetStash<AttackSequenceComponent>().Set(
                weapon,
                new AttackSequenceComponent
                {
                    Id = 0,
                    IsActive = false,
                    VisitedTargets = new System.Collections.Generic.List<
                        Scellecs.Morpeh.Entity>()
                });

            if (config.Type == WeaponType.Ripper)
            {
                _world.GetStash<RipperSequenceComponent>().Set(
                    weapon,
                    new RipperSequenceComponent());
            }

            EffectCollection effects = new EffectCollection();

            if (config.Effects != null)
            {
                foreach (EffectConfigs.EffectConfig effectConfig in config.Effects)
                {
                    IEffectRuntime effect = EffectRuntimeFactory.Create(effectConfig);

                    if (effect != null)
                        effects.Add(effect);
                    else if (effectConfig != null)
                    {
                        Debug.LogWarning(
                            $"Weapon {config.Type} has an unsupported effect config " +
                            $"'{effectConfig.name}'.",
                            effectConfig);
                    }
                }
            }

            _weaponEffectsStash.Set(weapon, new WeaponEffectsComponent
            {
                Effects = effects
            });

            _targetStash.Set(weapon, new TargetComponent());
            _positionStash.Set(weapon, new PositionComponent());
            return true;
        }
    }
}
