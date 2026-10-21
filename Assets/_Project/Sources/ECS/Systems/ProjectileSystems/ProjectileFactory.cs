using System.Collections.Generic;
using Core.Game;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileFactory
    {
        private readonly World _world;
        private readonly ProjectileDatabase _database;
        private readonly Transform _container;

        private readonly Dictionary<WeaponType, ObjectPool<AnimatedView>> _views =
            new Dictionary<WeaponType, ObjectPool<AnimatedView>>();

        private readonly Dictionary<WeaponType, IProjectileCreator> _creators =
            new Dictionary<WeaponType, IProjectileCreator>();

        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<ColliderComponent> _colliderStash;
        private readonly Stash<ProjectileHitComponent> _projectileHitStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;
        private readonly Stash<ProjectileViewComponent> _projectileViewStash;
        private readonly Stash<TargetComponent> _targetStash;

        public ProjectileFactory(
            World world,
            ProjectileDatabase database,
            Transform container)
        {
            _world = world;
            _database = database;
            _container = container;

            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();
            _projectileHitStash = world.GetStash<ProjectileHitComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
            _projectileViewStash = world.GetStash<ProjectileViewComponent>();
            _targetStash = world.GetStash<TargetComponent>();

            InitializeConfigs();
        }

        public void Begin(Entity weapon)
        {
            if (!_weaponStash.Has(weapon) ||
                !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon) ||
                !_creationStash.Has(weapon) ||
                !_attackStatusStash.Has(weapon))
            {
                return;
            }

            WeaponType weaponType = _weaponStash.Get(weapon).Type;

            if (!HasProjectileConfig(weaponType))
            {
                FinishCreation(weapon);
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);

            creation.ElapsedTime = 0f;
            creation.RemainingCount = Mathf.Max(0, attack.ProjectileCount);
            creation.IsInitialized = true;

            attackStatus.CreationFinished = false;

            if (creation.RemainingCount <= 0)
            {
                FinishCreation(weapon);
                return;
            }

            CreateRelease(weapon);
            creation.RemainingCount--;

            if (creation.RemainingCount <= 0)
                FinishCreation(weapon);
        }

        public void Update(Entity weapon, float deltaTime)
        {
            if (!_creationStash.Has(weapon) ||
                !_attackStatusStash.Has(weapon) ||
                !_attackStash.Has(weapon))
            {
                return;
            }

            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);

            if (!creation.IsInitialized || attackStatus.CreationFinished)
                return;

            if (!_weaponStash.Has(weapon) ||
                !HasProjectileConfig(_weaponStash.Get(weapon).Type))
            {
                FinishCreation(weapon);
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            if (creation.RemainingCount <= 0)
            {
                FinishCreation(weapon);
                return;
            }

            if (attack.FireRate <= 0f)
            {
                while (creation.RemainingCount > 0)
                {
                    CreateRelease(weapon);
                    creation.RemainingCount--;
                }

                FinishCreation(weapon);
                return;
            }

            creation.ElapsedTime += deltaTime;

            while (creation.ElapsedTime >= attack.FireRate &&
                   creation.RemainingCount > 0)
            {
                creation.ElapsedTime -= attack.FireRate;

                CreateRelease(weapon);
                creation.RemainingCount--;
            }

            if (creation.RemainingCount <= 0)
                FinishCreation(weapon);
        }

        public void Cancel(Entity weapon)
        {
            FinishCreation(weapon);
        }

        private void CreateRelease(Entity weapon)
        {
            if (!_weaponStash.Has(weapon) ||
                !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                return;
            }

            WeaponType weaponType = _weaponStash.Get(weapon).Type;
            ProjectileConfig config = GetConfig(weaponType);

            if (config == null || config.Collider == null)
                return;

            _creators.TryGetValue(
                weaponType,
                out IProjectileCreator creator);

            _views.TryGetValue(
                weaponType,
                out ObjectPool<AnimatedView> pool);

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            int projectileCount = Mathf.Max(1, attack.ShotsInAttack);
            Entity[] projectiles = new Entity[projectileCount];

            for (int i = 0; i < projectiles.Length; i++)
                projectiles[i] = CreateProjectile(weapon, config, pool);

            if (creator != null)
                creator.Create(weapon, projectiles);
        }

        private Entity CreateProjectile(
            Entity weapon,
            ProjectileConfig config,
            ObjectPool<AnimatedView> pool)
        {
            ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);
            ref AttackComponent attack = ref _attackStash.Get(weapon);

            Vector3 direction = GetDirection(weapon);

            Vector3 spawnPosition =
                _positionStash.Get(weapon).Position +
                direction * attack.DistanceFromSource;

            Entity projectile = _world.CreateEntity();

            _projectileStash.Set(projectile, new ProjectileComponent
            {
                Weapon = weapon,
                WeaponType = weaponComponent.Type,
                Speed = attack.ProjectileSpeed,
                Lifetime = attack.ProjectileLifetime,
                CurrentLifetime = 0f,
                Damage = attack.Damage,
                Pierce = attack.Pierce,
                RicochetsCount = 0,
                Direction = direction,
                TargetType = attack.TargetType,
                HitInterval = attack.HitInterval,
                HitPolicy = attack.HitPolicy,
                CleanupRequested = false
            });

            _positionStash.Set(projectile, new PositionComponent
            {
                Position = spawnPosition
            });

            _colliderStash.Set(projectile, new ColliderComponent
            {
                Type = config.Collider.Type,
                Radius = config.Collider.Radius,
                Length = config.Collider.Length,
                Size = config.Collider.Size,
                Angle = 0f,
                CapsuleOrientation = config.Collider.CapsuleOrientation
            });

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

        private void InitializeConfigs()
        {
            if (_database == null || _database.Projectiles == null)
                return;

            foreach (ProjectileConfig config in _database.Projectiles)
            {
                if (config == null || config.Collider == null)
                    continue;

                if (config.Prefab != null &&
                    !_views.ContainsKey(config.WeaponType))
                {
                    _views.Add(
                        config.WeaponType,
                        new ObjectPool<AnimatedView>(
                            config.Prefab,
                            config.PoolCapacity,
                            _container));
                }

                if (_creators.ContainsKey(config.WeaponType))
                    continue;

                IProjectileCreator creator = CreateCreator(config);

                if (creator != null)
                    _creators.Add(config.WeaponType, creator);
            }
        }

        private IProjectileCreator CreateCreator(ProjectileConfig config)
        {
            if (config is ArcProjectileConfig arcConfig)
                return new AxeProjectileCreator(_world, arcConfig);

            if (config is StraightProjectileConfig straightConfig)
                return new StraightProjectileCreator(_world, straightConfig);

            return null;
        }

        private ProjectileConfig GetConfig(WeaponType weaponType)
        {
            if (_database == null)
                return null;

            return _database.Get(weaponType);
        }

        private bool HasProjectileConfig(WeaponType weaponType)
        {
            ProjectileConfig config = GetConfig(weaponType);

            return config != null && config.Collider != null;
        }

        private Vector3 GetDirection(Entity weapon)
        {
            if (!_targetStash.Has(weapon))
                return Vector3.right;

            Entity target = _targetStash.Get(weapon).Target;

            if (target == default || !_positionStash.Has(target))
                return Vector3.right;

            Vector3 direction =
                _positionStash.Get(target).Position -
                _positionStash.Get(weapon).Position;

            direction.z = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return Vector3.right;

            return direction.normalized;
        }

        private void FinishCreation(Entity weapon)
        {
            if (_creationStash.Has(weapon))
            {
                ref ProjectileCreationComponent creation =
                    ref _creationStash.Get(weapon);

                creation.ElapsedTime = 0f;
                creation.RemainingCount = 0;
                creation.IsInitialized = false;
            }

            if (_attackStatusStash.Has(weapon))
                _attackStatusStash.Get(weapon).CreationFinished = true;
        }
    }
}