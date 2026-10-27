using System.Collections.Generic;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.AttackComponents;
using ECS.WeaponComponents;
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
        private const float StuckDetectionDelay = 0.25f;
        private const float AvoidanceCheckInterval = 0.1f;
        private const float AvoidanceSteeringStrength = 0.75f;
        private const float MovementEpsilonSqr = 0.000001f;

        private readonly MovementResolver _movementResolver;
        private readonly NavigationGrid _navigationGrid;
        private readonly SpatialGrid _spatialGrid;

        private readonly List<Entity> _neighbours = new List<Entity>();

        private Filter _enemyFilter;
        private Filter _playerFilter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<NavigationBoxComponent> _navigationBoxStash;
        private Stash<MovementLockComponent> _movementLockStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<EnemyMovementVariationComponent> _variationStash;
        private Stash<EnemyNavigationStateComponent> _navigationStateStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<EnemyMovementPolicyComponent> _policyStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<AttackComponent> _attackStash;
        private Filter _weaponFilter;

        public EnemyMovementSystem(
            NavigationGrid navigationGrid,
            SpatialGrid spatialGrid)
        {
            _navigationGrid = navigationGrid;
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
                .With<NavigationBoxComponent>()
                .With<EnemyMovementVariationComponent>()
                .With<EnemyNavigationStateComponent>()
                .With<EnemyMovementPolicyComponent>()
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
            _navigationBoxStash =
                World.GetStash<NavigationBoxComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _variationStash =
                World.GetStash<EnemyMovementVariationComponent>();
            _navigationStateStash =
                World.GetStash<EnemyNavigationStateComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _policyStash = World.GetStash<EnemyMovementPolicyComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _weaponFilter = World.Filter
                .With<WeaponComponent>()
                .With<AttackStatusComponent>()
                .With<AttackComponent>()
                .Build();
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
                {
                    ref var lockedMovement = ref _movementStash.Get(entity);
                    lockedMovement.Direction = Vector3.zero;
                    lockedMovement.ActualDisplacement = Vector3.zero;

                    ref var lockedNavigationState =
                        ref _navigationStateStash.Get(entity);
                    ResetNavigationState(ref lockedNavigationState);
                    continue;
                }

                ref var movement = ref _movementStash.Get(entity);
                ref var position = ref _positionStash.Get(entity);
                ref var stats = ref _statsStash.Get(entity);
                ref var navigationBox =
                    ref _navigationBoxStash.Get(entity);
                ref var variation = ref _variationStash.Get(entity);
                ref var navigationState =
                    ref _navigationStateStash.Get(entity);
                ref var policy = ref _policyStash.Get(entity);

                if (ShouldStopWhileAttacking(entity, policy.AttackPolicy))
                {
                    movement.Direction = Vector3.zero;
                    movement.ActualDisplacement = Vector3.zero;
                    ResetNavigationState(ref navigationState);
                    continue;
                }

                if (policy.MovePolicy == MovePolicyType.ApproachTarget &&
                    TryGetTarget(entity, out Entity attackTarget) &&
                    attackTarget != default &&
                    !_deathStash.Has(attackTarget))
                {
                    movement.Direction = Vector3.zero;
                    movement.ActualDisplacement = Vector3.zero;
                    ResetNavigationState(ref navigationState);
                    continue;
                }

                Vector3 targetPosition =
                    playerPosition + (Vector3)variation.ApproachOffset;
                float desiredAttackDistance = 0f;

                Vector3 toTarget = targetPosition - position.Position;
                toTarget.z = 0f;

                if (policy.MovePolicy == MovePolicyType.MaintainAttackDistance &&
                    policy.PreferredAttackDistance > 0f)
                {
                    Vector3 rangeTargetPosition = playerPosition;
                    if (TryGetTarget(entity, out Entity rangeTarget) &&
                        _positionStash.Has(rangeTarget))
                        rangeTargetPosition = _positionStash.Get(rangeTarget).Position;

                    targetPosition = rangeTargetPosition;

                    Vector3 targetOffset = rangeTargetPosition - position.Position;
                    targetOffset.z = 0f;
                    float distance = targetOffset.magnitude;
                    float desiredDistance = policy.PreferredAttackDistance;
                    desiredAttackDistance = desiredDistance;
                    if (distance > 0.001f)
                    {
                        if (distance < desiredDistance * 0.9f)
                            toTarget = -targetOffset.normalized *
                                       (desiredDistance - distance);
                        else if (distance <= desiredDistance * 1.1f)
                            toTarget = Vector3.zero;
                        else
                            toTarget = targetOffset.normalized * (distance - desiredDistance);
                    }
                }

                Vector3 targetDirection = Vector3.zero;

                if (toTarget.sqrMagnitude > StopDistance * StopDistance)
                    targetDirection = toTarget.normalized;

                Vector3 direction = targetDirection;

                if (navigationState.AvoidanceSide != 0 &&
                    targetDirection.sqrMagnitude > 0f)
                {
                    Vector3 tangent = new Vector3(
                        -targetDirection.y,
                        targetDirection.x,
                        0f);
                    direction += tangent * navigationState.AvoidanceSide *
                                 AvoidanceSteeringStrength;
                }

                Vector3 separation = CalculateSeparation(position.Position);

                direction += separation * SeparationStrength;

                if (direction.sqrMagnitude <= 0.0001f)
                {
                    movement.Direction = Vector3.zero;
                    movement.ActualDisplacement = Vector3.zero;
                    ResetNavigationState(ref navigationState);
                    continue;
                }

                direction.Normalize();

                Vector3 delta =
                    direction *
                    stats.Speed *
                    variation.SpeedMultiplier *
                    deltaTime;

                Vector3 previousPosition = position.Position;
                _movementResolver.Move(
                    ref position.Position,
                    delta,
                    navigationBox.Collider);

                Vector3 actualDisplacement = position.Position - previousPosition;
                actualDisplacement.z = 0f;
                movement.ActualDisplacement = actualDisplacement;
                movement.Direction = actualDisplacement.sqrMagnitude >
                                     MovementEpsilonSqr
                    ? actualDisplacement.normalized
                    : Vector3.zero;

                UpdateNavigationState(
                    ref navigationState,
                    position.Position,
                    targetPosition,
                    targetDirection,
                    desiredAttackDistance,
                    actualDisplacement,
                    navigationBox.Collider,
                    deltaTime);
            }
        }

        private void UpdateNavigationState(
            ref EnemyNavigationStateComponent navigationState,
            Vector3 position,
            Vector3 targetPosition,
            Vector3 targetDirection,
            float desiredAttackDistance,
            Vector3 actualDisplacement,
            ColliderComponent navigationCollider,
            float deltaTime)
        {
            if (targetDirection.sqrMagnitude <= 0f)
            {
                ResetNavigationState(ref navigationState);
                return;
            }

            float probeDistance = Mathf.Max(
                _navigationGrid.CellSize * 0.5f,
                StopDistance);
            float forwardProgress = Vector3.Dot(
                actualDisplacement,
                targetDirection);

            if (navigationState.AvoidanceSide != 0)
            {
                navigationState.AvoidanceCheckTime += deltaTime;
                if (navigationState.AvoidanceCheckTime < AvoidanceCheckInterval)
                    return;

                navigationState.AvoidanceCheckTime = 0f;
                if (_navigationGrid.IsWalkable(
                        position + targetDirection * probeDistance,
                        navigationCollider))
                {
                    navigationState.AvoidanceSide = 0;
                    navigationState.StuckTime = 0f;
                    return;
                }

                navigationState.StuckTime = Mathf.Min(
                    navigationState.StuckTime + deltaTime,
                    StuckDetectionDelay);
                return;
            }

            float displacementMagnitude = actualDisplacement.magnitude;
            bool madeForwardProgress = forwardProgress >
                                       Mathf.Max(0.0001f, displacementMagnitude * 0.1f);
            if (madeForwardProgress)
            {
                navigationState.StuckTime = 0f;
                return;
            }

            navigationState.StuckTime += deltaTime;

            if (navigationState.StuckTime < StuckDetectionDelay)
                return;

            bool directPathIsBlocked = !_navigationGrid.IsWalkable(
                position + targetDirection * probeDistance,
                navigationCollider);
            if (!directPathIsBlocked)
            {
                navigationState.StuckTime = 0f;
                return;
            }

            navigationState.AvoidanceSide = ChooseAvoidanceSide(
                position,
                targetPosition,
                targetDirection,
                desiredAttackDistance,
                navigationCollider,
                navigationState.PreferredSide,
                probeDistance);
        }

        private int ChooseAvoidanceSide(
            Vector3 position,
            Vector3 targetPosition,
            Vector3 targetDirection,
            float desiredAttackDistance,
            ColliderComponent navigationCollider,
            int preferredSide,
            float probeDistance)
        {
            Vector3 tangent = new Vector3(
                -targetDirection.y,
                targetDirection.x,
                0f);

            int bestSide = preferredSide == 0 ? 1 : preferredSide;
            float bestScore = float.MaxValue;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 sidePosition = position + tangent * side * probeDistance;
                Vector3 candidatePosition = sidePosition +
                                            targetDirection * probeDistance;

                bool sideIsWalkable = _navigationGrid.IsWalkable(
                    sidePosition,
                    navigationCollider);
                bool candidateIsWalkable = _navigationGrid.IsWalkable(
                    candidatePosition,
                    navigationCollider);

                float score = GetNavigationTargetError(
                    candidatePosition,
                    targetPosition,
                    desiredAttackDistance);

                if (!sideIsWalkable)
                    score += probeDistance * 4f;
                if (!candidateIsWalkable)
                    score += probeDistance * 2f;
                if (side != preferredSide)
                    score += probeDistance * 0.01f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestSide = side;
            }

            return bestSide;
        }

        private float GetNavigationTargetError(
            Vector3 candidatePosition,
            Vector3 targetPosition,
            float desiredAttackDistance)
        {
            float distance = Vector3.Distance(
                new Vector3(candidatePosition.x, candidatePosition.y, 0f),
                new Vector3(targetPosition.x, targetPosition.y, 0f));

            return desiredAttackDistance > 0f
                ? Mathf.Abs(distance - desiredAttackDistance)
                : distance;
        }

        private static void ResetNavigationState(
            ref EnemyNavigationStateComponent navigationState)
        {
            navigationState.StuckTime = 0f;
            navigationState.AvoidanceCheckTime = 0f;
            navigationState.AvoidanceSide = 0;
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

        private bool ShouldStopWhileAttacking(
            Entity owner,
            AttackPolicyType attackPolicy)
        {
            foreach (Entity weapon in _weaponFilter)
            {
                if (_weaponStash.Get(weapon).Owner != owner ||
                    !_attackStatusStash.Get(weapon).IsAttacking)
                    continue;

                if (attackPolicy == AttackPolicyType.StopWhileAttacking ||
                    _attackStash.Get(weapon).AnimationPolicy ==
                    AttackAnimationPolicyType.Animated)
                    return true;
            }
            return false;
        }

        private bool TryGetTarget(Entity owner, out Entity target)
        {
            foreach (Entity weapon in _weaponFilter)
            {
                if (_weaponStash.Get(weapon).Owner != owner ||
                    !_targetStash.Has(weapon))
                    continue;
                target = _targetStash.Get(weapon).Target;
                return target != default;
            }
            target = default;
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
                movement.ActualDisplacement = Vector3.zero;

                if (_navigationStateStash.Has(entity))
                {
                    ref var navigationState =
                        ref _navigationStateStash.Get(entity);
                    ResetNavigationState(ref navigationState);
                }
            }
        }

        public void Dispose()
        {
            _neighbours.Clear();
        }
    }
}
