using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class RipperTrajectorySystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<RipperTrajectoryComponent> _trajectoryStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<RipperTrajectoryComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<HitBoxComponent>()
                .Build();

            _projectileStash = World.GetStash<ProjectileComponent>();
            _trajectoryStash = World.GetStash<RipperTrajectoryComponent>();
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
                ref RipperTrajectoryComponent trajectory =
                    ref _trajectoryStash.Get(entity);

                if (projectile.CurrentLifetime < 0f)
                    continue;

                if (trajectory.Duration <= 0f ||
                    !_positionStash.Has(projectile.Weapon))
                {
                    continue;
                }

                trajectory.CurrentTime = Mathf.Min(
                    trajectory.Duration,
                    trajectory.CurrentTime + Mathf.Max(0f, deltaTime));

                float progress = Mathf.Clamp01(
                    trajectory.CurrentTime / trajectory.Duration);
                float sweepProgress = progress <= 0.5f
                    ? progress * 2f
                    : (1f - progress) * 2f;
                Vector3 start =
                    _positionStash.Get(projectile.Weapon).Position +
                    trajectory.StartOffset;

                ref PositionComponent position =
                    ref _positionStash.Get(entity);
                Vector3 perpendicular = new Vector3(
                    -trajectory.Direction.y,
                    trajectory.Direction.x,
                    0f);
                float shake = Mathf.Sin(
                    trajectory.CurrentTime * trajectory.ShakeFrequency *
                    Mathf.PI * 2f) * trajectory.ShakeAmplitude;
                position.Position = start +
                                    trajectory.Direction *
                                    (trajectory.SweepLength * sweepProgress) +
                                    perpendicular * shake;

                float angle = Mathf.Atan2(
                    trajectory.AttackDirection.y,
                    trajectory.AttackDirection.x) * Mathf.Rad2Deg;

                _rotationStash.Get(entity).Angle = angle;
                _hitBoxStash.Get(entity).Collider.Angle = angle;
            }
        }

        public void Dispose() { }
    }
}
