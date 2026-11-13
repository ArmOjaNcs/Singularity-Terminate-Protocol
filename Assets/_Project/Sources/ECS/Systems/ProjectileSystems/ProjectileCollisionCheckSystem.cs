using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.DamageComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.Spatial;
using Gameplay.View;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileCollisionCheckSystem : ISystem
    {
        public World World { get; set; }

        private readonly SpatialGrid _grid;
        private readonly List<Entity> _candidates = new List<Entity>();

        private Filter _filter;
        private Filter _enemyFilter;
        private Filter _playerFilter;

        private Stash<ProjectileComponent> _projectileStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<DamageRequest> _damageRequestStash;
        private Stash<CleanupRequest> _cleanupRequestStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;
        private Stash<ProjectileHitComponent> _projectileHitStash;
        private Stash<RicochetRequest> _ricochetRequestStash;

        public ProjectileCollisionCheckSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .With<HitBoxComponent>()
                .Build();

            _enemyFilter = World.Filter
                .With<EnemyTag>()
                .Without<DeathComponent>()
                .Build();

            _playerFilter = World.Filter
                .With<PlayerTag>()
                .Without<DeathComponent>()
                .Build();

            _projectileStash = World.GetStash<ProjectileComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _damageRequestStash = World.GetStash<DamageRequest>();
            _cleanupRequestStash = World.GetStash<CleanupRequest>();
            _projectileViewStash = World.GetStash<ProjectileViewComponent>();
            _projectileHitStash = World.GetStash<ProjectileHitComponent>();
            _ricochetRequestStash = World.GetStash<RicochetRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity projectile in _filter)
            {
                ref ProjectileComponent projectileComponent = ref _projectileStash.Get(projectile);

                if (!projectileComponent.CollisionEnabled ||
                    projectileComponent.CleanupRequested ||
                    projectileComponent.RicochetPending)
                    continue;

                bool suppressHitRegistration =
                    projectileComponent.CurrentLifetime <
                    projectileComponent.HitSuppressionUntilTime;

                ColliderComponent projectileCollider =
                    _hitBoxStash.Get(projectile).Collider;
                Vector3 projectilePosition =
                    ColliderGeometry.GetColliderWorldCenter(
                        _positionStash.Get(projectile).Position,
                        projectileCollider);

                ClearIgnoredTargetAfterSeparation(
                    projectile,
                    ref projectileComponent);

                float queryRadius = GetQueryRadius(projectile);

                _grid.Query(projectilePosition, queryRadius, _candidates);

                for (int i = 0; i < _candidates.Count; i++)
                {
                    Entity target = _candidates[i];

                    if (!IsValidTarget(target, projectileComponent))
                        continue;

                    bool overlaps = CheckCollision(projectile, target);
                    if (target == projectileComponent.TemporarilyIgnoredTarget)
                    {
                        if (overlaps)
                            continue;

                        projectileComponent.TemporarilyIgnoredTarget = default;
                    }

                    if (!overlaps)
                        continue;

                    // Keep all overlap detection in this system, but don't
                    // register a hit until the ricochet grace window ends.
                    if (suppressHitRegistration)
                        continue;

                    if (!TryRegisterHit(projectile, target))
                        continue;

                    CreateDamageRequest(projectile, target);

                    CreateRicochetRequest(projectile, target);
                    projectileComponent.RicochetPending =
                        projectileComponent.RicochetChance > 0f;

                    if (projectileComponent.HitPolicy != HitPolicyType.Pierce)
                    {
                        if (projectileComponent.RicochetPending)
                            break;
                        continue;
                    }

                    if (projectileComponent.Pierce <= 0)
                    {
                        RequestCleanup(projectile);
                        break;
                    }

                    projectileComponent.Pierce--;
                    if (projectileComponent.RicochetPending)
                        break;
                }
            }
        }

        private void ClearIgnoredTargetAfterSeparation(
            Entity projectile,
            ref ProjectileComponent projectileComponent)
        {
            Entity ignoredTarget = projectileComponent.TemporarilyIgnoredTarget;
            if (ignoredTarget == default)
                return;

            if (World.IsDisposed(ignoredTarget) ||
                !_hitBoxStash.Has(ignoredTarget) ||
                !_positionStash.Has(ignoredTarget) ||
                !CheckCollision(projectile, ignoredTarget))
            {
                projectileComponent.TemporarilyIgnoredTarget = default;
            }
        }

        private void CreateRicochetRequest(Entity projectile, Entity hitTarget)
        {
            ProjectileComponent projectileComponent =
                _projectileStash.Get(projectile);
            if (projectileComponent.RicochetChance <= 0f)
                return;
            Entity request = World.CreateEntity();

            _ricochetRequestStash.Set(request, new RicochetRequest
            {
                Projectile = projectile,
                Weapon = projectileComponent.Weapon,
                HitTarget = hitTarget,
                SequenceId = projectileComponent.SequenceId,
                SequencePolicy = projectileComponent.SequencePolicy,
                HitPosition = _positionStash.Has(hitTarget)
                    ? _positionStash.Get(hitTarget).Position
                    : _positionStash.Get(projectile).Position
            });
        }

        private bool TryRegisterHit(Entity projectile, Entity target)
        {
            if (!_projectileHitStash.Has(projectile))
                return false;

            ref ProjectileComponent projectileComponent =
                ref _projectileStash.Get(projectile);

            ref ProjectileHitComponent hitComponent =
                ref _projectileHitStash.Get(projectile);

            List<ProjectileHitRecord> records = hitComponent.Records;

            if (records == null)
            {
                records = new List<ProjectileHitRecord>();
                hitComponent.Records = records;
            }

            int recordIndex = records.FindIndex(
                record => record.Target == target);

            switch (projectileComponent.HitPolicy)
            {
                case HitPolicyType.Pierce:
                case HitPolicyType.OncePerTarget:
                    {
                        if (recordIndex >= 0)
                            return false;

                        records.Add(new ProjectileHitRecord
                        {
                            Target = target,
                            NextHitTime = 0f,
                            LastHitLeg = projectileComponent.RicochetLeg
                        });

                        break;
                    }

                case HitPolicyType.PeriodicPerTarget:
                    {
                        float currentTime = projectileComponent.CurrentLifetime;
                        float hitInterval = Mathf.Max(
                            0.01f,
                            projectileComponent.HitInterval);

                        if (recordIndex < 0)
                        {
                            records.Add(new ProjectileHitRecord
                            {
                                Target = target,
                                NextHitTime = currentTime + hitInterval
                            });

                            break;
                        }

                        ProjectileHitRecord record = records[recordIndex];

                        if (currentTime < record.NextHitTime)
                            return false;

                        record.NextHitTime = currentTime + hitInterval;
                        records[recordIndex] = record;
                        break;
                    }

                case HitPolicyType.OncePerTargetPerLeg:
                    {
                        if (recordIndex < 0)
                        {
                            records.Add(new ProjectileHitRecord
                            {
                                Target = target,
                                LastHitLeg = projectileComponent.RicochetLeg
                            });
                            break;
                        }

                        ProjectileHitRecord record = records[recordIndex];
                        if (record.LastHitLeg == projectileComponent.RicochetLeg)
                            return false;

                        record.LastHitLeg = projectileComponent.RicochetLeg;
                        records[recordIndex] = record;
                        break;
                    }

                default:
                    return false;
            }

            hitComponent.HitCount++;

            return true;
        }

        private float GetQueryRadius(Entity entity)
        {
            ColliderComponent collider = _hitBoxStash.Get(entity).Collider;

            switch (collider.Type)
            {
                case ColliderType.Circle:
                    return collider.Radius;

                case ColliderType.Box:
                    return collider.Size.magnitude * 0.5f;

                case ColliderType.Capsule:
                    return collider.Length * 0.5f +
                           collider.Radius;

                case ColliderType.Sector:
                    return collider.Radius;

                default:
                    return 0f;
            }
        }

        private bool CheckCollision(Entity projectile, Entity target)
        {
            ColliderComponent projectileCollider =
                _hitBoxStash.Get(projectile).Collider;
            ColliderComponent targetCollider =
                _hitBoxStash.Get(target).Collider;

            Vector3 projectilePosition =
                _positionStash.Get(projectile).Position;
            Vector3 targetPosition = _positionStash.Get(target).Position;

            return ColliderGeometry.Overlaps(
                projectilePosition,
                projectileCollider,
                targetPosition,
                targetCollider);
        }

        private bool IsValidTarget(Entity entity, ProjectileComponent projectile)
        {
            if (entity == projectile.Weapon)
                return false;

            if (!_hitBoxStash.Has(entity))
                return false;

            return projectile.TargetType switch
            {
                AttackTargetType.Enemy =>
                    _enemyFilter.Has(entity),
                
                AttackTargetType.Player =>
                    _playerFilter.Has(entity),

                _ => false
            };
        }

        private void CreateDamageRequest(Entity projectile, Entity target)
        {
            ref ProjectileComponent projectileComponent =
                ref _projectileStash.Get(projectile);

            Entity weapon = projectileComponent.Weapon;

            Entity owner = _weaponStash.Get(weapon).Owner;

            CreatureType sourceType = _statsStash.Get(owner).CreatureType;

            Entity requestEntity = World.CreateEntity();

            _damageRequestStash.Set(
                requestEntity,
                new DamageRequest
                {
                    Target = target,
                    Damage = projectileComponent.Damage,
                    ImpactPosition = _positionStash.Get(projectile).Position,
                    HasImpactPosition = true,
                    Context = new DamageContext
                    {
                        Weapon = weapon,
                        DamageType = DamageType.Direct,
                        SourceType = sourceType,
                        WeaponType = projectileComponent.WeaponType,
                    }
                });
        }

        private void RequestCleanup(Entity projectile)
        {
            ref ProjectileComponent projectileComponent =
                ref _projectileStash.Get(projectile);

            projectileComponent.CleanupRequested = true;

            if (_projectileViewStash.Has(projectile))
            {
                AnimatedView view =
                    _projectileViewStash.Get(projectile).View;

                if (view != null)
                    view.SetActive(false);
            }

            Entity request = World.CreateEntity();

            _cleanupRequestStash.Set(
                request,
                new CleanupRequest
                {
                    Candidate = projectile
                });
        }

        public void Dispose()
        {
        }
    }
}
