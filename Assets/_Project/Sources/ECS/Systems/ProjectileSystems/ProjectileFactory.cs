using System.Collections.Generic;
using Configs;
using Core.Game;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.Spatial;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileFactory
    {
        private readonly World _world;
        private readonly Transform _container;
        private readonly SpatialGrid _grid;
        private readonly Dictionary<WeaponType, Definition> _definitions = new();
        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ProjectileHitComponent> _projectileHitStash;
        private readonly Stash<ProjectileViewComponent> _projectileViewStash;

        public ProjectileFactory(World world, ProjectileDatabase database, Transform container, SpatialGrid grid)
        {
            _world = world;
            _container = container;
            _grid = grid;
            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _projectileHitStash = world.GetStash<ProjectileHitComponent>();
            _projectileViewStash = world.GetStash<ProjectileViewComponent>();

            if (database == null || database.Projectiles == null)
                return;

            foreach (ProjectileConfig config in database.Projectiles)
            {
                if (config == null || _definitions.ContainsKey(config.WeaponType))
                    continue;

                Definition definition = config.Collider == null
                    ? new Definition(config, null, null)
                    : new Definition(config, CreateCreator(config), CreateViewPool(config.Prefab, config.PoolCapacity));
                _definitions.Add(config.WeaponType, definition);
            }
        }

        internal bool TryGetDefinition(WeaponType type, out Definition definition)
        {
            return _definitions.TryGetValue(type, out definition) && definition.Config.Collider != null;
        }

        internal ObjectPool<AnimatedView> CreateViewPool(AnimatedView prefab, int capacity)
        {
            return prefab == null ? null : new ObjectPool<AnimatedView>(prefab, capacity, _container);
        }

        internal Entity Create(
            Entity weapon,
            in Definition definition,
            Vector3 direction,
            int sequenceId,
            int swingIndex)
        {
            ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);
            ref AttackComponent attack = ref _attackStash.Get(weapon);

            Vector3 spawnPosition =
                _positionStash.Get(weapon).Position +
                direction * attack.DistanceFromSource;

            var data = new ProjectileComponent
            {
                Weapon = weapon,
                WeaponType = weaponComponent.Type,
                Speed = attack.ProjectileSpeed,
                Lifetime = attack.ProjectileLifetime,
                CurrentLifetime = 0f,
                Damage = attack.Damage,
                Pierce = attack.Pierce,
                RicochetsCount = 0,
                RicochetLeg = 0,
                MaxRicochets = attack.MaxRicochets,
                RicochetChance = attack.RicochetChance,
                AllowRicochetRepeatTargets = attack.AllowRicochetRepeatTargets,
                TargetSearchRadius = attack.RicochetTargetSearchRadius,
                RicochetPolicy = attack.RicochetPolicy,
                SequenceId = sequenceId,
                CycleIndex = swingIndex,
                SwingIndex = swingIndex,
                Direction = direction,
                TargetTypes = attack.TargetTypes,
                HitInterval = attack.HitInterval,
                HitPolicy = attack.HitPolicy,
                CollisionEnabled = true,
                CleanupRequested = false,
                ViewActivated = true,
                AttackCompletionReported = false
            };
            ColliderConfig colliderConfig = definition.Config.Collider;
            var collider = new ColliderComponent
            {
                Type = colliderConfig.Type,
                Radius = colliderConfig.Radius,
                Length = colliderConfig.Length,
                Size = colliderConfig.Size,
                Center = colliderConfig.Center,
                CapsuleOrientation = colliderConfig.CapsuleOrientation
            };
            return Create(data, spawnPosition,
                ColliderScaler.Scale(collider, attack.ProjectileScale),
                attack.ProjectileScale, definition.Views);
        }

        internal Entity Create(
            ProjectileComponent data,
            Vector3 spawnPosition,
            ColliderComponent collider,
            float viewScale,
            ObjectPool<AnimatedView> pool)
        {
            Entity projectile = _world.CreateEntity();
            _projectileStash.Set(projectile, data);
            _positionStash.Set(projectile, new PositionComponent { Position = spawnPosition });
            _hitBoxStash.Set(projectile, new HitBoxComponent { Collider = collider });

            _projectileHitStash.Set(projectile, new ProjectileHitComponent
            {
                Records = new List<ProjectileHitRecord>(),
                HitCount = 0
            });

            if (pool != null)
            {
                AnimatedView view = pool.GetElement();

                if (view != null)
                {
                    view.SetEntity(projectile);
                    view.SetScaleMultiplier(viewScale);

                    _projectileViewStash.Set(
                        projectile,
                        new ProjectileViewComponent
                        {
                            View = view
                        });

                    view.SetActive(true);
                }
            }

            return projectile;
        }

        private IProjectileCreator CreateCreator(ProjectileConfig config)
        {
            if (config is TacticalComplexProjectileConfig tacticalConfig &&
                config.WeaponType == WeaponType.TacticalComplex)
            {
                return new TacticalComplexProjectileCreator(_world, tacticalConfig);
            }

            if (config is RocketLauncherProjectileConfig rocketConfig &&
                config.WeaponType == WeaponType.RocketLauncher)
            {
                return new RocketLauncherProjectileCreator(_world, rocketConfig);
            }

            if (config is GrenadeLauncherProjectileConfig grenadeConfig &&
                (config.WeaponType == WeaponType.GrenadeLauncher ||
                 config.WeaponType == WeaponType.Armageddon6))
            {
                return new GrenadeLauncherProjectileCreator(_world, grenadeConfig, _grid);
            }

            if (config.WeaponType == WeaponType.ElectricRifle || config.WeaponType == WeaponType.Volt)
                return new ElectricRifleProjectileCreator(_world);

            if (config is RipperProjectileConfig ripperConfig &&
                config.WeaponType.UsesRipperAttack())
            {
                return new RipperProjectileCreator(_world, ripperConfig);
            }

            if (config is ArcProjectileConfig &&
                config.WeaponType.UsesSwordAttack())
            {
                return new SwordProjectileCreator(_world);
            }

            if (config is ArcProjectileConfig arcConfig)
                return new AxeProjectileCreator(_world, arcConfig);

            if (config is StraightProjectileConfig straightConfig)
                return new StraightProjectileCreator(_world, straightConfig);

            return null;
        }

        internal readonly struct Definition
        {
            public readonly ProjectileConfig Config;
            public readonly IProjectileCreator Creator;
            public readonly ObjectPool<AnimatedView> Views;

            public Definition(ProjectileConfig config, IProjectileCreator creator, ObjectPool<AnimatedView> views)
            {
                Config = config;
                Creator = creator;
                Views = views;
            }
        }
    }
}
