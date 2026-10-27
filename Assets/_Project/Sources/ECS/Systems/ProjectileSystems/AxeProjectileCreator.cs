using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
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
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;

        public AxeProjectileCreator(
            World world,
            ArcProjectileConfig config)
        {
            _config = config;

            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _arcTrajectoryStash = world.GetStash<ArcTrajectoryComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            if (_config == null ||
                !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            foreach (Entity projectile in projectiles)
            {
                if (!_projectileStash.Has(projectile) ||
                    !_positionStash.Has(projectile))
                {
                    continue;
                }

                Vector3 direction =
                    _projectileStash.Get(projectile).Direction;

                Vector3 colliderOffset = _config.ColliderOffset;

                _rotationStash.Set(projectile, new RotationComponent
                {
                    Angle = 0f
                });

                _arcTrajectoryStash.Set(
                    projectile,
                    new ArcTrajectoryComponent
                    {
                        Direction = direction,
                        ColliderOffset = colliderOffset,
                        DistanceFromSource = attack.DistanceFromSource,
                        Radius = _config.TrajectoryRadius,
                        StartAngle = _config.StartAngle,
                        EndAngle = _config.EndAngle,
                        StartRotationAngle = _config.StartAngle,
                        EndRotationAngle = _config.EndAngle,
                        Duration = attack.AttackDuration,
                        CurrentTime = 0f,
                        BaseColliderCenter =
                            _hitBoxStash.Get(projectile).Collider.Center
                    });
            }
        }
    }
}
