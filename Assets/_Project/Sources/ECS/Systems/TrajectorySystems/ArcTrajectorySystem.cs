using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class ArcTrajectorySystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<ProjectileComponent> _projectileStash;
        private Stash<ArcTrajectoryComponent> _trajectoryStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<ArcTrajectoryComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<HitBoxComponent>()
                .Build();

            _projectileStash = World.GetStash<ProjectileComponent>();
            _trajectoryStash = World.GetStash<ArcTrajectoryComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(entity);

                ref ArcTrajectoryComponent trajectory =
                    ref _trajectoryStash.Get(entity);

                if (!_positionStash.Has(projectile.Weapon))
                    continue;

                if (trajectory.Duration <= 0f)
                    continue;

                ref PositionComponent ownerPosition =
                    ref _positionStash.Get(projectile.Weapon);

                trajectory.CurrentTime += deltaTime;

                float progress = Mathf.Clamp01(
                    trajectory.CurrentTime / trajectory.Duration);

                Vector3 direction = trajectory.Direction;

                direction.z = 0f;

                if (direction.sqrMagnitude <= 0.0001f)
                    continue;

                direction.Normalize();

                float startAngle = trajectory.StartAngle;
                float endAngle = trajectory.EndAngle;

                if (direction.x < 0f)
                {
                    startAngle = -trajectory.StartAngle;
                    endAngle = -trajectory.EndAngle;
                }

                float trajectoryAngle = Mathf.Lerp(
                    startAngle,
                    endAngle,
                    progress);

                float trajectoryRadians =
                    trajectoryAngle * Mathf.Deg2Rad;

                Vector3 perpendicular = new Vector3(
                    -direction.y,
                    direction.x,
                    0f);

                Vector3 origin =
                    ownerPosition.Position +
                    direction * trajectory.DistanceFromSource;

                Vector3 offset =
                    direction *
                    (Mathf.Cos(trajectoryRadians) * trajectory.Radius) +
                    perpendicular *
                    (Mathf.Sin(trajectoryRadians) * trajectory.Radius);

                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                position.Position = origin + offset;

                float rotationOffset = Mathf.Lerp(
                    trajectory.StartRotationAngle,
                    trajectory.EndRotationAngle,
                    progress);

                if (direction.x < 0f)
                    rotationOffset = -rotationOffset;

                float attackAngle = Mathf.Atan2(
                    direction.y,
                    direction.x) * Mathf.Rad2Deg;

                float rotationAngle = attackAngle + rotationOffset;

                ref RotationComponent rotation =
                    ref _rotationStash.Get(entity);

                rotation.Angle = rotationAngle;

                ref ColliderComponent collider =
                    ref _hitBoxStash.Get(entity).Collider;

                collider.Angle = rotationAngle;

                collider.Center = trajectory.BaseColliderCenter +
                                  new Vector2(
                                      trajectory.ColliderOffset.x,
                                      trajectory.ColliderOffset.y);
            }
        }

        public void Dispose()
        {
        }
    }
}
