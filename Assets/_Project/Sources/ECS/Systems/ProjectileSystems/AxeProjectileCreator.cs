using Core.Game;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class AxeProjectileCreator : IProjectileCreator
    {
        private readonly ArcProjectileConfig _config;

        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private readonly Stash<ColliderPositionComponent> _colliderPositionStash;
        private readonly Stash<ProjectileViewComponent> _projectileViewStash;

        private readonly ObjectPool<AnimatedView> _axeViews;

        public AxeProjectileCreator(
            World world,
            ProjectileConfig config,
            Transform container)
        {
            _config = config as ArcProjectileConfig;

            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _arcTrajectoryStash = world.GetStash<ArcTrajectoryComponent>();
            _colliderPositionStash = world.GetStash<ColliderPositionComponent>();
            _projectileViewStash = world.GetStash<ProjectileViewComponent>();

            _axeViews = new ObjectPool<AnimatedView>(
                config.Prefab,
                config.PoolCapacity,
                container);
        }

        public void Configure(Entity weapon, Entity projectile, Vector3 direction)
        {
            ref AttackComponent attack = ref _attackStash.Get(weapon);

            Vector3 colliderOffset = _config.ColliderOffset;

            _rotationStash.Set(projectile, new RotationComponent
            {
                Angle = 0f
            });

            _arcTrajectoryStash.Set(projectile, new ArcTrajectoryComponent
            {
                Direction = direction,
                ColliderOffset = colliderOffset,
                DistanceFromSource = attack.DistanceFromSource,
                Radius = _config.TrajectoryRadius,
                StartAngle = _config.StartAngle,
                EndAngle = _config.EndAngle,
                StartRotationAngle = -45f,
                EndRotationAngle = 45f,
                Duration = attack.AttackDuration,
                CurrentTime = 0f
            });

            _colliderPositionStash.Set(projectile, new ColliderPositionComponent
            {
                CenterPosition = _positionStash.Get(projectile).Position + colliderOffset
            });

            AnimatedView view = _axeViews.GetElement();

            if (view == null)
                return;

            view.SetEntity(projectile);

            _projectileViewStash.Set(projectile, new ProjectileViewComponent
            {
                View = view
            });

            view.SetActive(true);
        }
    }
}