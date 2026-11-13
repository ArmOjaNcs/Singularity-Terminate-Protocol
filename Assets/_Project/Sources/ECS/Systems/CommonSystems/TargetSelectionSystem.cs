using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using Gameplay;
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
        private Filter _damageableFilter;
        private Stash<PositionComponent> _positionStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<AttackSequenceComponent> _sequenceStash;
        private Stash<HitBoxComponent> _hitBoxStash;

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

            _damageableFilter = World.Filter
                .With<DamageableTag>()
                .Without<DeathComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _sequenceStash = World.GetStash<AttackSequenceComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
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

                AttackSequenceComponent sequence = _sequenceStash.Has(entity)
                    ? _sequenceStash.Get(entity)
                    : default;

                if (sequence.IsActive &&
                    attack.ProjectileSequencePolicy ==
                    ProjectileSequencePolicy.ComboOnTarget &&
                    sequence.LockedTarget != default)
                {
                    if (CommonFunctions.IsValidTarget(sequence.LockedTarget, attack.TargetTypes, _damageableFilter, _enemyFilter, _playerFilter) &&
                        _positionStash.Has(sequence.LockedTarget) &&
                        _hitBoxStash.Has(sequence.LockedTarget))
                    {
                        target.Target = sequence.LockedTarget;
                    }
                    else
                    {
                        target.Target = default;
                    }

                    continue;
                }

                FindTarget(
                    position.Position,
                    attack.TargetSearchRadius,
                    attack.Trajectory.FlightDistance > 0f
                        ? attack.DistanceFromSource + attack.Trajectory.FlightDistance
                        : 0f,
                    attack.Trajectory.FlightDistanceTolerance,
                    attack.TargetTypes,
                    sequence.IsActive ? sequence.VisitedTargets : null,
                    ref target);
            }
        }

        private void FindTarget(
            Vector3 position,
            float radius,
            float preferredDistance,
            float distanceTolerance,
            AttackTargetType[] targetType,
            List<Entity> excludedTargets,
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

                if (excludedTargets != null &&
                    excludedTargets.Contains(candidate))
                {
                    continue;
                }

                if (!CommonFunctions.IsValidTarget(candidate, targetType, _damageableFilter, _enemyFilter, _playerFilter))
                    continue;

                if (!_positionStash.Has(candidate) ||
                    !_hitBoxStash.Has(candidate))
                {
                    continue;
                }

                ColliderComponent collider =
                    _hitBoxStash.Get(candidate).Collider;

                Vector3 candidatePosition =
                    _positionStash.Get(candidate).Position;

                float distance = preferredDistance > 0f
                    ? Vector3.Distance(position, candidatePosition)
                    : ColliderGeometry.DistanceToCollider(
                        position,
                        candidatePosition,
                        collider);

                if (preferredDistance > 0f)
                {
                    if (distance > radius)
                        continue;

                    float distanceError = Mathf.Abs(distance - preferredDistance);
                    if (distanceError > distanceTolerance ||
                        distanceError >= closestDistance)
                    {
                        continue;
                    }

                    closestDistance = distanceError;
                    closestEntity = candidate;
                    continue;
                }

                if (distance > radius || distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestEntity = candidate;
            }

            target.Target = closestEntity;
        }

        public void Dispose()
        {
        }
    }
}
