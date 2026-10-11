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
        private Stash<ColliderComponent> _colliderStash;
        private Stash<ColliderPositionComponent> _colliderPositionStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<ArcTrajectoryComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<ColliderComponent>()
                .With<ColliderPositionComponent>()
                .Build();

            _projectileStash = World.GetStash<ProjectileComponent>();
            _trajectoryStash = World.GetStash<ArcTrajectoryComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _colliderStash = World.GetStash<ColliderComponent>();
            _colliderPositionStash =
                World.GetStash<ColliderPositionComponent>();
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

                float trajectoryAngle = Mathf.Lerp(
                    trajectory.StartAngle,
                    trajectory.EndAngle,
                    progress);

                float trajectoryRadians =
                    trajectoryAngle * Mathf.Deg2Rad;

                Vector3 direction = trajectory.Direction.normalized;

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

                float attackAngle = Mathf.Atan2(
                    direction.y,
                    direction.x) * Mathf.Rad2Deg;

                float rotationAngle = attackAngle + rotationOffset;

                ref RotationComponent rotation =
                    ref _rotationStash.Get(entity);

                rotation.Angle = rotationAngle;

                ref ColliderComponent collider =
                    ref _colliderStash.Get(entity);

                collider.Angle = rotationAngle;

                ref ColliderPositionComponent colliderPosition =
                    ref _colliderPositionStash.Get(entity);

                Vector3 colliderOffset =
                    Quaternion.Euler(0f, 0f, rotationAngle) *
                    trajectory.ColliderOffset;

                colliderPosition.CenterPosition =
                    position.Position + colliderOffset;
            }
        }

        public void Dispose()
        {
        }
    }
}