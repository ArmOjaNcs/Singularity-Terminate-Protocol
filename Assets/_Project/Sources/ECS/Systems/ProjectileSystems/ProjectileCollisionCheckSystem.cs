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
        private Stash<ColliderComponent> _colliderStash;
        private Stash<ColliderPositionComponent> _colliderPositionStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<DamageRequest> _damageRequestStash;
        private Stash<CleanupRequest> _cleanupRequestStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;
        private Stash<ProjectileHitComponent> _projectileHitStash;

        public ProjectileCollisionCheckSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .With<ColliderComponent>()
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
            _colliderStash = World.GetStash<ColliderComponent>();
            _colliderPositionStash = World.GetStash<ColliderPositionComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _damageRequestStash = World.GetStash<DamageRequest>();
            _cleanupRequestStash = World.GetStash<CleanupRequest>();
            _projectileViewStash = World.GetStash<ProjectileViewComponent>();
            _projectileHitStash = World.GetStash<ProjectileHitComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity projectile in _filter)
            {
                ref ProjectileComponent projectileComponent = ref _projectileStash.Get(projectile);

                if (projectileComponent.CleanupRequested)
                    continue;

                Vector3 projectilePosition = GetColliderPosition(projectile);

                float queryRadius = GetQueryRadius(projectile);

                _grid.Query(projectilePosition, queryRadius, _candidates);

                for (int i = 0; i < _candidates.Count; i++)
                {
                    Entity target = _candidates[i];

                    if (!IsValidTarget(target, projectileComponent))
                        continue;

                    if (!CheckCollision(projectile, target))
                        continue;

                    if (!TryRegisterHit(projectile, target))
                        continue;

                    CreateDamageRequest(projectile, target);

                    if (projectileComponent.HitPolicy != HitPolicyType.Pierce)
                        continue;

                    if (projectileComponent.Pierce <= 0)
                    {
                        RequestCleanup(projectile);
                        break;
                    }

                    projectileComponent.Pierce--;
                }
            }
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
                            NextHitTime = 0f
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

                default:
                    return false;
            }

            hitComponent.HitCount++;

            return true;
        }

        private Vector3 GetColliderPosition(Entity entity)
        {
            if (_colliderPositionStash.Has(entity))
                return _colliderPositionStash.Get(entity).CenterPosition;

            return _positionStash.Get(entity).Position;
        }

        private float GetQueryRadius(Entity entity)
        {
            ref ColliderComponent collider = ref _colliderStash.Get(entity);

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
            ref ColliderComponent projectileCollider = ref _colliderStash.Get(projectile);
            ref ColliderComponent targetCollider = ref _colliderStash.Get(target);

            Vector3 projectilePosition = GetColliderPosition(projectile);
            Vector3 targetPosition = GetColliderPosition(target);

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

            if (!_colliderStash.Has(entity))
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