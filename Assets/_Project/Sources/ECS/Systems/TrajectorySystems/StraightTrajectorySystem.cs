using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class StraightTrajectorySystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<ProjectileComponent> _projectileStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<StraightTrajectoryComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<HitBoxComponent>()
                .Build();

            _projectileStash = World.GetStash<ProjectileComponent>();
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

                Vector3 direction = projectile.Direction;
                direction.z = 0f;

                if (direction.sqrMagnitude <= 0.0001f)
                    continue;

                direction.Normalize();
                projectile.Direction = direction;

                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                position.Position +=
                    direction * projectile.Speed * deltaTime;

                float angle = Mathf.Atan2(
                    direction.y,
                    direction.x) * Mathf.Rad2Deg;

                ref RotationComponent rotation =
                    ref _rotationStash.Get(entity);

                rotation.Angle = angle;

                ref ColliderComponent collider =
                    ref _hitBoxStash.Get(entity).Collider;

                collider.Angle = angle;
            }
        }

        public void Dispose()
        {
        }
    }
}