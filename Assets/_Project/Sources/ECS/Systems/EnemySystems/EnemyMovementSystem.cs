using System.Collections.Generic;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using Gameplay;
using Gameplay.Navigation;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.EnemySystems
{
    public sealed class EnemyMovementSystem : ISystem
    {
        private const float SeparationRadius = 1.1f;
        private const float SeparationStrength = 1.5f;
        private const float StopDistance = 0.15f;

        private readonly MovementResolver _movementResolver;
        private readonly SpatialGrid _spatialGrid;

        private readonly List<Entity> _neighbours = new List<Entity>();

        private Filter _enemyFilter;
        private Filter _playerFilter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<ColliderComponent> _colliderStash;
        private Stash<MovementLockComponent> _movementLockStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<EnemyMovementVariationComponent> _variationStash;
        private Stash<DeathComponent> _deathStash;

        public EnemyMovementSystem(
            NavigationGrid navigationGrid,
            SpatialGrid spatialGrid)
        {
            _movementResolver = new MovementResolver(navigationGrid);
            _spatialGrid = spatialGrid;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _enemyFilter = World.Filter
                .With<EnemyTag>()
                .With<MovementComponent>()
                .With<PositionComponent>()
                .With<StatsComponent>()
                .With<ColliderComponent>()
                .With<EnemyMovementVariationComponent>()
                .Without<DeathComponent>()
                .Build();

            _playerFilter = World.Filter
                .With<PlayerTag>()
                .With<PositionComponent>()
                .Without<DeathComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _colliderStash = World.GetStash<ColliderComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _variationStash =
                World.GetStash<EnemyMovementVariationComponent>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (!TryGetPlayerPosition(out Vector3 playerPosition))
            {
                StopEnemies();
                return;
            }

            foreach (Entity entity in _enemyFilter)
            {
                if (_movementLockStash.Has(entity))
                    continue;

                ref var movement = ref _movementStash.Get(entity);
                ref var position = ref _positionStash.Get(entity);
                ref var stats = ref _statsStash.Get(entity);
                ref var collider = ref _colliderStash.Get(entity);
                ref var variation = ref _variationStash.Get(entity);

                Vector3 targetPosition =
                    playerPosition + (Vector3)variation.ApproachOffset;

                Vector3 toTarget = targetPosition - position.Position;
                toTarget.z = 0f;

                Vector3 direction = Vector3.zero;

                if (toTarget.sqrMagnitude > StopDistance * StopDistance)
                    direction = toTarget.normalized;

                Vector3 separation = CalculateSeparation(position.Position);

                direction += separation * SeparationStrength;

                if (direction.sqrMagnitude <= 0.0001f)
                {
                    movement.Direction = Vector3.zero;
                    continue;
                }

                direction.Normalize();
                movement.Direction = direction;

                Vector3 delta =
                    direction *
                    stats.Speed *
                    variation.SpeedMultiplier *
                    deltaTime;

                _movementResolver.Move(
                    ref position.Position,
                    delta,
                    collider);
            }
        }

        private Vector3 CalculateSeparation(Vector3 position)
        {
            _spatialGrid.Query(
                position,
                SeparationRadius,
                _neighbours);

            Vector3 separation = Vector3.zero;
            float radiusSqr = SeparationRadius * SeparationRadius;

            for (int i = 0; i < _neighbours.Count; i++)
            {
                Entity neighbour = _neighbours[i];

                if (!_enemyTagStash.Has(neighbour) ||
                    _deathStash.Has(neighbour) ||
                    !_positionStash.Has(neighbour))
                {
                    continue;
                }

                Vector3 offset =
                    position - _positionStash.Get(neighbour).Position;

                offset.z = 0f;

                float distanceSqr = offset.sqrMagnitude;

                if (distanceSqr <= 0.0001f ||
                    distanceSqr >= radiusSqr)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(distanceSqr);
                float weight = 1f - distance / SeparationRadius;

                separation += offset / distance * weight;
            }

            return separation;
        }

        private bool TryGetPlayerPosition(out Vector3 playerPosition)
        {
            foreach (Entity entity in _playerFilter)
            {
                playerPosition = _positionStash.Get(entity).Position;
                return true;
            }

            playerPosition = Vector3.zero;
            return false;
        }

        private void StopEnemies()
        {
            foreach (Entity entity in _enemyFilter)
            {
                if (_movementLockStash.Has(entity))
                    continue;

                ref var movement = ref _movementStash.Get(entity);
                movement.Direction = Vector3.zero;
            }
        }

        public void Dispose()
        {
            _neighbours.Clear();
        }
    }
}