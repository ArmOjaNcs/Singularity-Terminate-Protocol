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
        private readonly Stash<TurretDeploymentComponent> _turretDeploymentStash;

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
            _turretDeploymentStash = world.GetStash<TurretDeploymentComponent>();
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

            if (config.Type.IsTurret() &&
                (config is not TurretWeaponConfig turret || !turret.HasViewSprites()))
            {
                Debug.LogError($"Cannot create {config.Type} without a base sprite and eight head sprites.", config);
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

            _weaponEffectsStash.Set(weapon, new WeaponEffectsComponent
            {
                Effects = EffectRuntimeFactory.CreateCollection(config)
            });

            _positionStash.Set(weapon, new PositionComponent());

            if (config.Type.IsTurret() && config is TurretWeaponConfig deployment)
            {
                _attackStash.Get(weapon).CurrentCooldown = deployment.DeploymentCooldown;
                _turretDeploymentStash.Set(weapon, new TurretDeploymentComponent { Config = deployment });
                return true;
            }

            InitializeAttackState(weapon);

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

            return true;
        }

        internal Entity CreateDeployedTurret(Entity deployment, Vector3 position)
        {
            // Copy before adding to the same stashes, which can grow during Set.
            WeaponComponent weapon = _weaponStash.Get(deployment);
            AttackComponent attack = _attackStash.Get(deployment);
            WeaponEffectsComponent effects = _weaponEffectsStash.Get(deployment);
            attack.CurrentCooldown = 0f;
            attack.Pattern.SpreadAngle = 0f;

            if (weapon.Type == WeaponType.ElectricTurret)
            {
                attack.ProjectileSpeed = 0f;
                attack.ProjectileLifetime = Mathf.Max(0.01f, attack.Trajectory.Duration);
                attack.CompletionPolicy = AttackCompletionPolicy.AfterTrajectories;
            }

            Entity entity = _world.CreateEntity();
            _weaponStash.Set(entity, weapon);
            _attackStash.Set(entity, attack);
            _weaponEffectsStash.Set(entity, effects);
            _positionStash.Set(entity, new PositionComponent { Position = position });
            InitializeAttackState(entity);
            return entity;
        }

        private void InitializeAttackState(Entity weapon)
        {
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

            _targetStash.Set(weapon, new TargetComponent());
        }
    }
}
