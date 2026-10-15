using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class TargetSelectionSystem : ISystem
    {
        private readonly SpatialGrid _grid;
        private readonly List<Entity> _candidates = new List<Entity>();

        private Filter _filter;
        private Filter _enemyFilter;
        private Filter _playerFilter;

        private Stash<PositionComponent> _positionStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<ColliderComponent> _colliderStash;
        private Stash<ColliderPositionComponent> _colliderPositionStash;

        public TargetSelectionSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<TargetComponent>()
                .With<AttackComponent>()
                .Without<DeathComponent>()
                .Build();

            _enemyFilter = World.Filter
                .With<EnemyTag>()
                .Without<DeathComponent>()
                .Build();

            _playerFilter = World.Filter
                .With<PlayerTag>()
                .Without<DeathComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _colliderStash = World.GetStash<ColliderComponent>();
            _colliderPositionStash =
                World.GetStash<ColliderPositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                ref AttackComponent attack =
                    ref _attackStash.Get(entity);

                ref TargetComponent target =
                    ref _targetStash.Get(entity);

                FindTarget(
                    position.Position,
                    attack.TargetSearchRadius,
                    attack.TargetType,
                    ref target);
            }
        }

        private void FindTarget(
            Vector3 position,
            float radius,
            AttackTargetType targetType,
            ref TargetComponent target)
        {
            target.Target = default;

            if (radius <= 0f)
                return;

            _grid.Query(position, radius, _candidates);

            Entity closestEntity = default;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < _candidates.Count; i++)
            {
                Entity candidate = _candidates[i];

                if (!IsValidTarget(candidate, targetType))
                    continue;

                if (!_positionStash.Has(candidate) ||
                    !_colliderStash.Has(candidate))
                {
                    continue;
                }

                Vector3 colliderPosition =
                    GetColliderPosition(candidate);

                ref ColliderComponent collider =
                    ref _colliderStash.Get(candidate);

                float distance = ColliderGeometry.DistanceToCollider(
                    position,
                    colliderPosition,
                    collider);

                if (distance > radius || distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestEntity = candidate;
            }

            target.Target = closestEntity;
        }

        private Vector3 GetColliderPosition(Entity entity)
        {
            if (_colliderPositionStash.Has(entity))
            {
                return _colliderPositionStash
                    .Get(entity)
                    .CenterPosition;
            }

            return _positionStash.Get(entity).Position;
        }

        private bool IsValidTarget(
            Entity entity,
            AttackTargetType targetType)
        {
            return targetType switch
            {
                AttackTargetType.Enemy =>
                    _enemyFilter.Has(entity),

                AttackTargetType.Player =>
                    _playerFilter.Has(entity),

                _ => false
            };
        }

        public void Dispose()
        {
        }
    }
}
