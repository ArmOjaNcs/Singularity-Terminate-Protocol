using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileFactory
    {
        private readonly World _world;
        private readonly ProjectileDatabase _database;

        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<ColliderComponent> _colliderStash;
        private readonly Stash<ProjectileHitComponent> _projectileHitStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;

        private readonly IProjectileCreator _axeCreator;

        public ProjectileFactory(
            World world,
            ProjectileDatabase database,
            Transform container)
        {
            _world = world;
            _database = database;

            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();
            _projectileHitStash = world.GetStash<ProjectileHitComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();

            ProjectileConfig axeConfig = database.Get(WeaponType.Axe);

            if (TryGetConfig<ArcProjectileConfig>(axeConfig, out ArcProjectileConfig config))
                _axeCreator = new AxeProjectileCreator(world, config, container);
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

            ProjectileConfig config = GetConfig(weapon);

            if (config == null)
            {
                FinishCreation(weapon);
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

            creation.ElapsedTime = 0f;
            creation.RemainingCount = Mathf.Max(0, attack.ProjectileCount);
            creation.IsInitialized = true;

            attackStatus.CreationFinished = false;

            if (creation.RemainingCount <= 0)
            {
                FinishCreation(weapon);
                return;
            }

            CreateProjectile(weapon, config);
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

            ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

            if (!creation.IsInitialized || attackStatus.CreationFinished)
                return;

            ProjectileConfig config = GetConfig(weapon);

            if (config == null)
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
                    CreateProjectile(weapon, config);
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

                CreateProjectile(weapon, config);
                creation.RemainingCount--;
            }

            if (creation.RemainingCount <= 0)
                FinishCreation(weapon);
        }

        public void Cancel(Entity weapon)
        {
            FinishCreation(weapon);
        }

        private Entity CreateProjectile(Entity weapon, ProjectileConfig config)
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
                Records = new System.Collections.Generic.List<ProjectileHitRecord>(),
                HitCount = 0
            });

            if (config.WeaponType == WeaponType.Axe)
                _axeCreator.Configure(weapon, projectile, direction);

            return projectile;
        }

        private ProjectileConfig GetConfig(Entity weapon)
        {
            if (!_weaponStash.Has(weapon))
                return null;

            return _database.Get(_weaponStash.Get(weapon).Type);
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

            if (direction.sqrMagnitude <= 0f)
                return Vector3.right;

            return direction.normalized;
        }

        private void FinishCreation(Entity weapon)
        {
            if (_creationStash.Has(weapon))
            {
                ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);

                creation.ElapsedTime = 0f;
                creation.RemainingCount = 0;
                creation.IsInitialized = false;
            }

            if (_attackStatusStash.Has(weapon))
                _attackStatusStash.Get(weapon).CreationFinished = true;
        }

        private bool TryGetConfig<T>(
            ProjectileConfig projectileConfig,
            out T config) 
            where T : ProjectileConfig
        {
            if (projectileConfig == null ||
                projectileConfig is not T typedConfig ||
                typedConfig.Prefab == null)
            {
                config = default;
                return false;
            }
           
            config = typedConfig;
            return true;
        }
    }
}