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

                    CreateDamageRequest(projectile, target);

                    if (projectileComponent.Pierce <= 0)
                    {
                        RequestCleanup(projectile);
                        break;
                    }

                    projectileComponent.Pierce--;
                }
            }
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

            Vector3 projectilePosition = GetColliderPosition(projectile);

            Vector3 targetPosition = GetColliderPosition(target);

            float targetRadius = GetQueryRadius(target);

            switch (projectileCollider.Type)
            {
                case ColliderType.Circle:
                    return CheckCircle(
                        projectilePosition,
                        projectileCollider,
                        targetPosition,
                        targetRadius);

                case ColliderType.Box:
                    return CheckBox(
                        projectilePosition,
                        projectileCollider,
                        targetPosition,
                        targetRadius);

                case ColliderType.Capsule:
                    return CheckCapsule(
                        projectilePosition,
                        projectileCollider,
                        targetPosition,
                        targetRadius);

                case ColliderType.Sector:
                    return CheckSector(
                        projectilePosition,
                        projectileCollider,
                        targetPosition,
                        targetRadius);

                default:
                    return false;
            }
        }

        private bool CheckCircle(
            Vector3 position,
            ColliderComponent collider,
            Vector3 targetPosition,
            float targetRadius)
        {
            Vector2 offset = new Vector2(
                targetPosition.x - position.x,
                targetPosition.y - position.y);

            float distance = offset.magnitude;

            return distance <= collider.Radius + targetRadius;
        }

        private bool CheckBox(
            Vector3 position,
            ColliderComponent collider,
            Vector3 targetPosition,
            float targetRadius)
        {
            Vector2 offset = new Vector2(
                targetPosition.x - position.x,
                targetPosition.y - position.y);

            Vector2 localTarget = Rotate(offset, -collider.Angle);

            float halfWidth = collider.Size.x * 0.5f;

            float halfHeight = collider.Size.y * 0.5f;

            return Mathf.Abs(localTarget.x) <=
                       halfWidth + targetRadius &&
                   Mathf.Abs(localTarget.y) <=
                       halfHeight + targetRadius;
        }

        private bool CheckCapsule(
            Vector3 position,
            ColliderComponent collider,
            Vector3 targetPosition,
            float targetRadius)
        {
            float radians = collider.Angle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            float halfSegmentLength = Mathf.Max(0f, collider.Length * 0.5f - collider.Radius);

            Vector2 center = new Vector2(position.x, position.y);

            Vector2 target = new Vector2(targetPosition.x, targetPosition.y);

            Vector2 pointA = center - direction * halfSegmentLength;

            Vector2 pointB = center + direction * halfSegmentLength;

            float distance = DistanceToSegment(target, pointA, pointB);

            return distance <= collider.Radius + targetRadius;
        }

        private bool CheckSector(
            Vector3 position,
            ColliderComponent collider,
            Vector3 targetPosition,
            float targetRadius)
        {
            Vector2 center = new Vector2(position.x, position.y);

            Vector2 offset = new Vector2(targetPosition.x, targetPosition.y) - center;

            float distance = offset.magnitude;

            if (distance > collider.Radius + targetRadius)
                return false;

            if (distance <= 0f)
                return true;

            float radians = collider.Angle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            float angle = Vector2.Angle(direction, offset);

            float sectorAngle = collider.Size.x * 0.5f;

            return angle <= sectorAngle;
        }

        private float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;

            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= 0f)
                return Vector2.Distance(point, start);

            float t = Vector2.Dot(point - start, segment) / lengthSquared;

            t = Mathf.Clamp01(t);

            Vector2 closest = start + segment * t;

            return Vector2.Distance(point, closest);
        }

        private Vector2 Rotate(Vector2 value, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                value.x * cos -
                value.y * sin,
                value.x * sin +
                value.y * cos);
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
                        ProjectileType = projectileComponent.Type
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