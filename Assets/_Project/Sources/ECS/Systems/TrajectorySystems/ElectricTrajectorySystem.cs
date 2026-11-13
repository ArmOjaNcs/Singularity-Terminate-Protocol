using System.Collections.Generic;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
using ECS.ProjectileSystems;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class ElectricTrajectorySystem : ISystem
    {
        private const float SteeringDegreesPerSecond = 720f;
        private readonly SpatialGrid _grid;
        private readonly List<Entity> _candidates = new List<Entity>();
        private RicochetTargetSelector _targetSelector;
        private Filter _filter;
        private Filter _enemyFilter;
        private Stash<ProjectileComponent> _projectiles;
        private Stash<ElectricTrajectoryComponent> _trajectories;
        private Stash<PositionComponent> _positions;
        private Stash<RotationComponent> _rotations;
        private Stash<HitBoxComponent> _hitBoxes;

        public ElectricTrajectorySystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter.With<ProjectileComponent>()
                .With<ElectricTrajectoryComponent>().With<PositionComponent>()
                .With<RotationComponent>().With<HitBoxComponent>().Build();
            _projectiles = World.GetStash<ProjectileComponent>();
            _trajectories = World.GetStash<ElectricTrajectoryComponent>();
            _positions = World.GetStash<PositionComponent>();
            _rotations = World.GetStash<RotationComponent>();
            _hitBoxes = World.GetStash<HitBoxComponent>();
            _enemyFilter = World.Filter.With<EnemyTag>()
                .Without<DeathComponent>().Build();
            _targetSelector = new RicochetTargetSelector(World, _grid);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref ProjectileComponent projectile = ref _projectiles.Get(entity);
                ref ElectricTrajectoryComponent trajectory = ref _trajectories.Get(entity);
                if (projectile.CurrentLifetime < 0f)
                {
                    projectile.CollisionEnabled = false;
                    continue;
                }

                projectile.CollisionEnabled = true;
                if (projectile.CleanupRequested)
                    continue;

                Vector3 currentPosition = _positions.Get(entity).Position;

                if (!HasLivingTarget(trajectory.Target))
                {
                    if (TryRetarget(entity, ref projectile, ref trajectory,
                            currentPosition))
                    {
                        currentPosition = _positions.Get(entity).Position;
                    }
                    else
                    {
                        MoveAlongCurrentDirection(entity, ref projectile,
                            ref trajectory, deltaTime);
                        continue;
                    }
                }

                Vector3 targetPosition =
                    _positions.Get(trajectory.Target).Position;
                if (trajectory.IsDetouring)
                {
                    Vector3 detourOffset =
                        trajectory.RicochetWaypoint - currentPosition;
                    detourOffset.z = 0f;

                    if (detourOffset.sqrMagnitude <= 0.09f)
                    {
                        trajectory.IsDetouring = false;
                    }
                    else
                    {
                        trajectory.ForwardDirection =
                            detourOffset.normalized;
                        MoveProjectile(entity, ref projectile, ref trajectory,
                            detourOffset.normalized, deltaTime,
                            useZigZag: false);
                        continue;
                    }
                }

                Vector3 offset = targetPosition - currentPosition;
                offset.z = 0f;
                if (offset.sqrMagnitude <= 0.0001f)
                    offset = trajectory.ForwardDirection.sqrMagnitude > 0.0001f
                        ? trajectory.ForwardDirection
                        : projectile.Direction.sqrMagnitude > 0.0001f
                            ? projectile.Direction
                            : Vector3.right;

                Vector3 desiredDirection = offset.normalized;
                if (trajectory.ForwardDirection.sqrMagnitude <= 0.0001f)
                {
                    trajectory.ForwardDirection = desiredDirection;
                }
                else
                {
                    trajectory.ForwardDirection = Vector3.RotateTowards(
                        trajectory.ForwardDirection.normalized,
                        desiredDirection,
                        SteeringDegreesPerSecond * Mathf.Deg2Rad * deltaTime,
                        0f).normalized;
                }

                MoveProjectile(entity, ref projectile, ref trajectory,
                    trajectory.ForwardDirection, deltaTime, useZigZag: true);
            }
        }

        private bool HasLivingTarget(Entity target)
        {
            if (target == default || World.IsDisposed(target))
                return false;

            return _enemyFilter.Has(target) && _positions.Has(target);
        }

        private bool TryRetarget(
            Entity projectileEntity,
            ref ProjectileComponent projectile,
            ref ElectricTrajectoryComponent trajectory,
            Vector3 currentPosition)
        {
            bool selected = _targetSelector.TrySelectNearest(
                projectileEntity,
                default,
                currentPosition,
                projectile.TargetSearchRadius,
                _candidates,
                null,
                false,
                out Entity target,
                out Vector3 direction);

            if (!selected && projectile.AllowRicochetRepeatTargets)
            {
                selected = _targetSelector.TrySelectNearest(
                    projectileEntity,
                    default,
                    currentPosition,
                    projectile.TargetSearchRadius,
                    _candidates,
                    null,
                    true,
                    out target,
                    out direction);
            }

            trajectory.IsDetouring = false;
            if (!selected)
                return false;

            trajectory.Target = target;
            projectile.Direction = direction;
            trajectory.ForwardDirection = direction;
            return true;
        }

        private void MoveAlongCurrentDirection(
            Entity entity,
            ref ProjectileComponent projectile,
            ref ElectricTrajectoryComponent trajectory,
            float deltaTime)
        {
            Vector3 direction = trajectory.ForwardDirection.sqrMagnitude > 0.0001f
                ? trajectory.ForwardDirection
                : projectile.Direction;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.right;

            MoveProjectile(entity, ref projectile, ref trajectory,
                direction.normalized, deltaTime, useZigZag: true);
        }

        private void MoveProjectile(
            Entity entity,
            ref ProjectileComponent projectile,
            ref ElectricTrajectoryComponent trajectory,
            Vector3 forward,
            float deltaTime,
            bool useZigZag)
        {
            if (useZigZag)
            {
                Vector3 perpendicular = new Vector3(-forward.y, forward.x, 0f);
                trajectory.Phase += deltaTime *
                                     Mathf.Max(0f, trajectory.Frequency) *
                                     Mathf.PI * 2f;
                Vector3 velocity = forward + perpendicular *
                    (Mathf.Sin(trajectory.Phase) *
                     Mathf.Max(0f, trajectory.Amplitude));
                projectile.Direction = velocity.normalized;
            }
            else
            {
                projectile.Direction = forward.normalized;
            }

            _positions.Get(entity).Position +=
                projectile.Direction * projectile.Speed * deltaTime;

            float angle = Mathf.Atan2(
                projectile.Direction.y,
                projectile.Direction.x) * Mathf.Rad2Deg;
            _rotations.Get(entity).Angle = angle;
            _hitBoxes.Get(entity).Collider.Angle = angle;
        }

        public void Dispose()
        {
            _candidates.Clear();
        }
    }
}
