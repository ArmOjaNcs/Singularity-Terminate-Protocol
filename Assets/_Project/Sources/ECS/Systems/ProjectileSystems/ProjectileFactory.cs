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
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<RipperSequenceComponent> _ripperSequenceStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ProjectileHitComponent> _projectileHitStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;
        private readonly Stash<ProjectileViewComponent> _projectileViewStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<DeathComponent> _deathStash;

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
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _ripperSequenceStash = world.GetStash<RipperSequenceComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _projectileHitStash = world.GetStash<ProjectileHitComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
            _projectileViewStash = world.GetStash<ProjectileViewComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _deathStash = world.GetStash<DeathComponent>();

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
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);

            creation.ElapsedTime = 0f;
            creation.RemainingCount = Mathf.Max(0, attack.ProjectileCount);
            creation.IsInitialized = true;

            sequence.Id++;
            sequence.IsActive = true;
            sequence.LockedTarget = default;
            sequence.ReleasesCreated = 0;
            sequence.OutstandingProjectiles = 0;
            sequence.RicochetsCount = 0;
            sequence.RicochetPending = false;
            sequence.PendingTarget = default;
            if (sequence.VisitedTargets == null)
                sequence.VisitedTargets = new List<Entity>();
            else
                sequence.VisitedTargets.Clear();

            attackStatus.CreationFinished = false;

            if (creation.RemainingCount <= 0)
            {
                if (attack.ProjectileSequencePolicy ==
                    ProjectileSequencePolicy.ComboOnTarget)
                    FinishCombo(weapon);
                else
                    FinishCreation(weapon);
                return;
            }

            if (!CreateRelease(weapon))
                return;

            creation.RemainingCount--;

            if (creation.RemainingCount <= 0)
            {
                if (attack.ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget)
                    FinishCreation(weapon);
            }
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

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            if (!_weaponStash.Has(weapon) ||
                !HasProjectileConfig(_weaponStash.Get(weapon).Type))
            {
                if (attack.ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget)
                    FinishCreation(weapon);
                return;
            }

            if (creation.RemainingCount <= 0)
            {
                if (attack.ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget)
                    FinishCreation(weapon);
                return;
            }

            if (attack.FireRate <= 0f)
            {
                while (creation.RemainingCount > 0)
                {
                    if (!CreateRelease(weapon))
                        return;

                    creation.RemainingCount--;
                }

                if (attack.ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget)
                    FinishCreation(weapon);
                return;
            }

            creation.ElapsedTime += deltaTime;

            while (creation.ElapsedTime >= attack.FireRate &&
                   creation.RemainingCount > 0)
            {
                creation.ElapsedTime -= attack.FireRate;

                if (!CreateRelease(weapon))
                    return;

                creation.RemainingCount--;
            }

            if (creation.RemainingCount <= 0)
            {
                if (attack.ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget)
                    FinishCreation(weapon);
            }
        }

        public void Cancel(Entity weapon)
        {
            FinishCreation(weapon);
        }

        private bool CreateRelease(Entity weapon)
        {
            if (!_weaponStash.Has(weapon) ||
                !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                FinishCreation(weapon);
                return false;
            }

            WeaponType weaponType = _weaponStash.Get(weapon).Type;
            ProjectileConfig config = GetConfig(weaponType);

            if (config == null || config.Collider == null)
            {
                FinishCreation(weapon);
                return false;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);

            Entity target = sequence.LockedTarget;
            if (attack.ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget ||
                target == default)
            {
                target = _targetStash.Has(weapon)
                    ? _targetStash.Get(weapon).Target
                    : default;
            }

            if (!TryGetDirection(weapon, target, out Vector3 direction))
            {
                bool hasCachedRipperDirection =
                    weaponType == WeaponType.Ripper &&
                    _ripperSequenceStash.Has(weapon) &&
                    _ripperSequenceStash.Get(weapon).IsInitialized &&
                    _ripperSequenceStash.Get(weapon).SequenceId == sequence.Id;

                if (!hasCachedRipperDirection)
                {
                    if (attack.ProjectileSequencePolicy ==
                            ProjectileSequencePolicy.ComboOnTarget &&
                        sequence.LockedTarget != default)
                    {
                        creation.RemainingCount = 0;
                        return false;
                    }

                    FinishCreation(weapon);
                    return false;
                }

                direction =
                    _ripperSequenceStash.Get(weapon).AttackDirection;
            }

            _creators.TryGetValue(
                weaponType,
                out IProjectileCreator creator);

            _views.TryGetValue(
                weaponType,
                out ObjectPool<AnimatedView> pool);

            int projectileCount = weaponType == WeaponType.Sword ||
                                  weaponType == WeaponType.Ripper
                ? 1
                : Mathf.Max(1, attack.ShotsInAttack);
            Entity[] projectiles = new Entity[projectileCount];

            if (attack.ProjectileSequencePolicy ==
                    ProjectileSequencePolicy.ComboOnTarget &&
                sequence.LockedTarget == default)
            {
                sequence.LockedTarget = target;
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                projectiles[i] = CreateProjectile(
                    weapon,
                    config,
                    pool,
                    direction,
                    sequence.Id,
                    sequence.ReleasesCreated);
            }

            if (creator != null)
                creator.Create(weapon, projectiles);

            if (target != default &&
                !sequence.VisitedTargets.Contains(target))
                sequence.VisitedTargets.Add(target);

            sequence.ReleasesCreated++;
            if (attack.ProjectileSequencePolicy ==
                ProjectileSequencePolicy.ComboOnTarget)
            {
                sequence.OutstandingProjectiles += projectiles.Length;
            }

            return true;
        }

        private Entity CreateProjectile(
            Entity weapon,
            ProjectileConfig config,
            ObjectPool<AnimatedView> pool,
            Vector3 direction,
            int sequenceId,
            int swingIndex)
        {
            ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);
            ref AttackComponent attack = ref _attackStash.Get(weapon);

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
                MaxRicochets = attack.MaxRicochets,
                RicochetChance = attack.RicochetChance,
                TargetSearchRadius = attack.TargetSearchRadius,
                SequencePolicy = attack.ProjectileSequencePolicy,
                SequenceId = sequenceId,
                SwingIndex = swingIndex,
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

            _hitBoxStash.Set(projectile, new HitBoxComponent
            {
                Collider = new ColliderComponent
                {
                    Type = config.Collider.Type,
                    Radius = config.Collider.Radius,
                    Length = config.Collider.Length,
                    Size = config.Collider.Size,
                    Center = config.Collider.Center,
                    Angle = 0f,
                    CapsuleOrientation = config.Collider.CapsuleOrientation
                }
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
            if (config is RipperProjectileConfig ripperConfig &&
                config.WeaponType == WeaponType.Ripper)
            {
                return new RipperProjectileCreator(_world, ripperConfig);
            }

            if (config is ArcProjectileConfig swordArcConfig &&
                config.WeaponType == WeaponType.Sword)
            {
                return new SwordProjectileCreator(_world, swordArcConfig);
            }

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

        private bool TryGetDirection(
            Entity weapon,
            Entity target,
            out Vector3 direction)
        {
            direction = Vector3.zero;

            if (target == default ||
                !_positionStash.Has(target) ||
                _deathStash.Has(target))
            {
                return false;
            }

            direction =
                _positionStash.Get(target).Position -
                _positionStash.Get(weapon).Position;

            direction.z = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.zero;
                return false;
            }

            direction.Normalize();
            return true;
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

            if (_sequenceStash.Has(weapon))
                _sequenceStash.Get(weapon).IsActive = false;
        }

        private void FinishCombo(Entity weapon)
        {
            if (_creationStash.Has(weapon))
            {
                ref ProjectileCreationComponent creation =
                    ref _creationStash.Get(weapon);
                creation.RemainingCount = 0;
            }
        }
    }
}
