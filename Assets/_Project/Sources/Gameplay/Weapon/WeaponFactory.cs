using System.Collections.Generic;
using Configs;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EffectComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
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
            return Create(config, owner, out _);
        }

        public bool Create(WeaponConfig config, Entity owner, out Entity createdWeapon)
        {
            createdWeapon = default;

            if (config == null)
            {
                Debug.LogError("Cannot create a weapon because its WeaponConfig is missing.");
                return false;
            }

            AttackConfig attackConfig = config.AttackConfig;
            if (attackConfig == null)
            {
                Debug.LogError(
                    $"Cannot create weapon {config.Type}: its AttackConfig is missing.");
                return false;
            }

            Entity weapon = _world.CreateEntity();
            createdWeapon = weapon;

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
                    attackConfig.CyclesPerAttack,
                    0,
                    Mathf.Max(1, attackConfig.MaxCyclesPerAttack)),
                ShotsPerCycle = Mathf.Clamp(
                    attackConfig.ShotsPerCycle,
                    0,
                    Mathf.Max(1, attackConfig.MaxShotsPerCycle)),
                MaxCyclesPerAttack = Mathf.Max(1, attackConfig.MaxCyclesPerAttack),
                MaxShotsPerCycle = Mathf.Max(1, attackConfig.MaxShotsPerCycle),
                Pierce = attackConfig.Pierce,
                MaxRicochets = attackConfig.MaxRicochets,

                CycleInterval = attackConfig.CycleInterval,
                ShotInterval = attackConfig.ShotInterval,
                Damage = attackConfig.Damage,
                AttackCooldown = attackConfig.AttackCooldown,
                CurrentCooldown = attackConfig.AttackCooldown,
                TargetSearchRadius = attackConfig.TargetSearchRadius,
                DistanceFromSource = attackConfig.DistanceFromSource,
                ProjectileScale = attackConfig.ProjectileScale,
                ScaleAttackGeometryWithProjectile =
                    attackConfig.ScaleAttackGeometryWithProjectile,
                Trajectory = new TrajectoryComponent
                {
                    Duration = attackConfig.Trajectory.Duration,
                    Revolutions = attackConfig.Trajectory.Revolutions,
                    FlightDistance = attackConfig.Trajectory.FlightDistance,
                    FlightDistanceTolerance = attackConfig.Trajectory.FlightDistanceTolerance,
                    ZigZagAmplitude = attackConfig.Trajectory.ZigZagAmplitude,
                    ZigZagFrequency = attackConfig.Trajectory.ZigZagFrequency,
                    ShakeAmplitude = attackConfig.Trajectory.ShakeAmplitude,
                    ShakeFrequency = attackConfig.Trajectory.ShakeFrequency
                },
                Pattern = new AttackPatternComponent
                {
                    SpreadAngle = attackConfig.Pattern.SpreadAngle,
                    RandomDirectionFallback = attackConfig.Pattern.RandomDirectionFallback,
                    SelectTargetPerCycle = attackConfig.Pattern.SelectTargetPerCycle
                },
                HomingEnabled = attackConfig.HomingEnabled,
                RicochetTargetSearchRadius = attackConfig.RicochetTargetSearchRadius,
                ProjectileSpeed = attackConfig.ProjectileSpeed,
                ProjectileLifetime = attackConfig.ProjectileLifetime,
                RicochetChance = attackConfig.RicochetChance,
                AllowRicochetRepeatTargets = attackConfig.AllowRicochetRepeatTargets,
                TargetTypes = attackConfig.TargetType,
                HitPolicy = attackConfig.HitPolicy,
                HitInterval = attackConfig.HitInterval,
                AnimationPolicy = attackConfig.AnimationPolicy,
                CompletionPolicy = attackConfig.CompletionPolicy,
                RicochetPolicy = attackConfig.RicochetPolicy
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

            _world.GetStash<AttackSequenceComponent>().Set(weapon, new AttackSequenceComponent
            {
                VisitedTargets = new List<Entity>()
            });

            if (config.Type.UsesRipperAttack())
                _world.GetStash<RipperSequenceComponent>().Add(weapon);

            if (config.Type.UsesSwordAttack())
            {
                _world.GetStash<SwordAttackComponent>().Set(weapon, new SwordAttackComponent
                {
                    ThrustDistance = attackConfig.ThrustDistance,
                    ZigZagWidth = attackConfig.ZigZagWidth
                });
            }

            if (config.Type == WeaponType.Volt)
            {
                _world.GetStash<VoltAttackComponent>().Set(weapon, new VoltAttackComponent
                {
                    HitsPerEmpWave = attackConfig.HitsPerEmpWave
                });
            }

            _weaponEffectsStash.Set(weapon, new WeaponEffectsComponent
            {
                Effects = EffectRuntimeFactory.CreateCollection(config)
            });

            _targetStash.Set(weapon, new TargetComponent());
            _positionStash.Set(weapon, new PositionComponent());
            return true;
        }
    }
}
