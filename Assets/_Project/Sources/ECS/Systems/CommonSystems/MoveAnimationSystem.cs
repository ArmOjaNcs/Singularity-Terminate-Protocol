using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.EnemyComponents;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.ViewComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class MoveAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<AttackComponent> _attackStash;
        private Filter _weaponFilter;
        private readonly Dictionary<Entity, EnemyFacingState> _enemyFacing =
            new Dictionary<Entity, EnemyFacingState>();

        private const float FacingDeadZone = 0.08f;
        private const float FacingSwitchConfirmDuration = 0.12f;
        private const float FacingMinimumAccumulatedDisplacement = 0.01f;

        private struct EnemyFacingState
        {
            public bool FacingLeft;
            public bool PendingFacingLeft;
            public float PendingDuration;
            public float PendingDisplacement;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<MovementComponent>()
                .With<AnimatedCreatureComponent>()
                .Without<DeathComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _animatedCreatureStash =
                World.GetStash<AnimatedCreatureComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _positionStash = World.GetStash<PositionComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _weaponFilter = World.Filter
                .With<WeaponComponent>()
                .With<TargetComponent>()
                .With<AttackStatusComponent>()
                .With<AttackComponent>()
                .Build();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref MovementComponent movement =
                    ref _movementStash.Get(entity);

                AnimatedCreature view =
                    _animatedCreatureStash.Get(entity).View;

                if (view == null)
                    continue;

                bool isMoving =
                    movement.Direction.sqrMagnitude > 0f;

                view.SetMoving(isMoving);

                if (_enemyTagStash.Has(entity))
                {
                    if (IsAnimatedAttack(entity))
                    {
                        UpdateEnemyFacing(entity, view);
                    }
                    else
                    {
                        UpdateEnemyFacingByMovement(
                            entity,
                            view,
                            movement.ActualDisplacement,
                            deltaTime);
                    }
                }
                else if (movement.Direction.x > 0f)
                    view.SetFacingLeft(false);
                else if (movement.Direction.x < 0f)
                    view.SetFacingLeft(true);
            }
        }

        private bool IsAnimatedAttack(Entity enemy)
        {
            foreach (Entity weapon in _weaponFilter)
            {
                if (_weaponStash.Get(weapon).Owner == enemy &&
                    _attackStatusStash.Get(weapon).IsAttacking &&
                    _attackStash.Get(weapon).AnimationPolicy ==
                    AttackAnimationPolicyType.Animated)
                    return true;
            }

            return false;
        }

        private void UpdateEnemyFacingByMovement(
            Entity enemy,
            AnimatedCreature view,
            Vector3 actualDisplacement,
            float deltaTime)
        {
            if (!_enemyFacing.TryGetValue(enemy, out EnemyFacingState state))
            {
                state.FacingLeft = view.IsXFlipped;
                state.PendingFacingLeft = state.FacingLeft;
            }

            float displacementMagnitude = actualDisplacement.magnitude;
            if (displacementMagnitude <= 0f)
            {
                ResetPendingFacing(ref state);
                _enemyFacing[enemy] = state;
                return;
            }

            float horizontalRatio = actualDisplacement.x / displacementMagnitude;
            if (Mathf.Abs(horizontalRatio) <= FacingDeadZone)
            {
                ResetPendingFacing(ref state);
                _enemyFacing[enemy] = state;
                return;
            }

            bool candidateFacingLeft = horizontalRatio < 0f;
            if (candidateFacingLeft == state.FacingLeft)
            {
                ResetPendingFacing(ref state);
                _enemyFacing[enemy] = state;
                return;
            }

            if (candidateFacingLeft != state.PendingFacingLeft)
            {
                state.PendingFacingLeft = candidateFacingLeft;
                state.PendingDuration = 0f;
                state.PendingDisplacement = 0f;
            }

            state.PendingDuration += deltaTime;
            state.PendingDisplacement += Mathf.Abs(actualDisplacement.x);

            if (state.PendingDuration >= FacingSwitchConfirmDuration &&
                state.PendingDisplacement >= FacingMinimumAccumulatedDisplacement)
            {
                state.FacingLeft = candidateFacingLeft;
                view.SetFacingLeft(candidateFacingLeft);
                ResetPendingFacing(ref state);
            }

            _enemyFacing[enemy] = state;
        }

        private static void ResetPendingFacing(ref EnemyFacingState state)
        {
            state.PendingFacingLeft = state.FacingLeft;
            state.PendingDuration = 0f;
            state.PendingDisplacement = 0f;
        }

        private void UpdateEnemyFacing(Entity enemy, AnimatedCreature view)
        {
            ResetEnemyFacingPending(enemy, view.IsXFlipped);

            if (!_positionStash.Has(enemy))
                return;

            foreach (Entity weapon in _weaponFilter)
            {
                if (_weaponStash.Get(weapon).Owner != enemy)
                    continue;

                Entity target = _targetStash.Get(weapon).Target;
                if (target == default || !_positionStash.Has(target))
                    return;

                float horizontalOffset =
                    _positionStash.Get(target).Position.x -
                    _positionStash.Get(enemy).Position.x;

                if (horizontalOffset > FacingDeadZone && view.IsXFlipped)
                    SetEnemyFacing(enemy, view, false);
                else if (horizontalOffset < -FacingDeadZone &&
                         !view.IsXFlipped)
                    SetEnemyFacing(enemy, view, true);

                return;
            }
        }

        private void ResetEnemyFacingPending(Entity enemy, bool facingLeft)
        {
            if (!_enemyFacing.TryGetValue(enemy, out EnemyFacingState state))
                state = new EnemyFacingState();

            state.FacingLeft = facingLeft;
            ResetPendingFacing(ref state);
            _enemyFacing[enemy] = state;
        }

        private void SetEnemyFacing(
            Entity enemy,
            AnimatedCreature view,
            bool facingLeft)
        {
            if (view.IsXFlipped == facingLeft &&
                _enemyFacing.TryGetValue(enemy, out EnemyFacingState state) &&
                state.FacingLeft == facingLeft)
            {
                return;
            }

            if (!_enemyFacing.TryGetValue(enemy, out state))
                state = new EnemyFacingState();

            state.FacingLeft = facingLeft;
            ResetPendingFacing(ref state);
            _enemyFacing[enemy] = state;
            view.SetFacingLeft(facingLeft);
        }

        public void Dispose()
        {
            _enemyFacing.Clear();
        }
    }
}
