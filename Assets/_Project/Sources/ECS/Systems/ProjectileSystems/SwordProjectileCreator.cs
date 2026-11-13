using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class SwordProjectileCreator : IProjectileCreator
    {
        private readonly World _world;
        private readonly ArcProjectileConfig _config;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private readonly Stash<SwordSlashComponent> _swordSlashStash;

        public SwordProjectileCreator(World world, ArcProjectileConfig config)
        {
            _world = world;
            _config = config;
            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _arcTrajectoryStash = world.GetStash<ArcTrajectoryComponent>();
            _swordSlashStash = world.GetStash<SwordSlashComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            if (_config == null ||
                !_attackStash.Has(weapon) ||
                !_sequenceStash.Has(weapon))
            {
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);

            Entity target = sequence.LockedTarget;
            if (target == default && _targetStash.Has(weapon))
                target = _targetStash.Get(weapon).Target;

            Vector3 fallbackDirection = Vector3.right;
            if (projectiles.Length > 0 &&
                _projectileStash.Has(projectiles[0]))
            {
                fallbackDirection = _projectileStash.Get(projectiles[0]).Direction;
                fallbackDirection.z = 0f;
                if (fallbackDirection.sqrMagnitude > 0.0001f)
                    fallbackDirection.Normalize();
            }

            Vector3 attackAreaPosition = target != default &&
                                         !_world.IsDisposed(target) &&
                                         _positionStash.Has(target)
                ? _positionStash.Get(target).Position
                : _positionStash.Get(weapon).Position +
                  fallbackDirection * attack.TargetSearchRadius;

            for (int index = 0; index < projectiles.Length; index++)
            {
                Entity projectileEntity = projectiles[index];
                if (!_projectileStash.Has(projectileEntity) ||
                    !_positionStash.Has(projectileEntity) ||
                    !_hitBoxStash.Has(projectileEntity))
                {
                    continue;
                }

                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(projectileEntity);
                float startDelay = attack.ShotInterval * index;
                Vector3 direction = projectile.Direction;
                direction.z = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                    continue;
                direction.Normalize();

                int swingIndex = projectile.SwingIndex + index;
                bool reverseSwing = swingIndex % 2 != 0;
                bool mirrorY = reverseSwing ^ (direction.x < 0f);
                float startAngle = reverseSwing
                    ? -_config.StartAngle
                    : _config.StartAngle;
                float endAngle = reverseSwing
                    ? -_config.EndAngle
                    : _config.EndAngle;

                _rotationStash.Set(projectileEntity, new RotationComponent());
                _arcTrajectoryStash.Set(projectileEntity,
                    new ArcTrajectoryComponent
                    {
                        Direction = direction,
                        Anchor = ArcTrajectoryAnchor.FixedPosition,
                        AnchorPosition = attackAreaPosition,
                        ColliderOffset = _config.ColliderOffset,
                        DistanceFromSource = attack.DistanceFromSource,
                        Radius = _config.TrajectoryRadius,
                        StartAngle = startAngle,
                        EndAngle = endAngle,
                        StartRotationAngle = startAngle,
                        EndRotationAngle = endAngle,
                        Duration = attack.Trajectory.Duration,
                        CurrentTime = -startDelay,
                        BaseColliderCenter =
                            _hitBoxStash.Get(projectileEntity).Collider.Center
                    });

                projectile.CurrentLifetime = -startDelay;
                if (startDelay > 0f)
                    projectile.CollisionEnabled = false;

                _swordSlashStash.Set(projectileEntity,
                    new SwordSlashComponent
                    {
                        SequenceId = projectile.SequenceId,
                        SwingIndex = projectile.SwingIndex,
                        MirrorY = mirrorY,
                        CompletionReported = false,
                        SequencePolicy = projectile.SequencePolicy
                    });
            }
        }
    }
}
