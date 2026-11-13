using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class RipperProjectileCreator : IProjectileCreator
    {
        public const float MotionDurationMultiplier = 1.25f;

        private readonly World _world;
        private readonly RipperProjectileConfig _config;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<RipperSequenceComponent> _ripperSequenceStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<RipperTrajectoryComponent> _trajectoryStash;

        public RipperProjectileCreator(
            World world,
            RipperProjectileConfig config)
        {
            _world = world;
            _config = config;
            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _ripperSequenceStash = world.GetStash<RipperSequenceComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _trajectoryStash = world.GetStash<RipperTrajectoryComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            if (_config == null ||
                !_attackStash.Has(weapon) ||
                !_sequenceStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref RipperSequenceComponent ripperSequence =
                ref _ripperSequenceStash.Get(weapon);

            if (!ripperSequence.IsInitialized ||
                ripperSequence.SequenceId != sequence.Id)
            {
                Entity target = sequence.LockedTarget;
                if (target == default && _targetStash.Has(weapon))
                    target = _targetStash.Get(weapon).Target;

                Vector3 targetOffset;
                if (target != default &&
                    !_world.IsDisposed(target) &&
                    _positionStash.Has(target))
                {
                    targetOffset = _positionStash.Get(target).Position -
                                   _positionStash.Get(weapon).Position;
                }
                else if (projectiles.Length > 0 &&
                         _projectileStash.Has(projectiles[0]))
                {
                    targetOffset = _projectileStash.Get(projectiles[0]).Direction;
                }
                else
                {
                    return;
                }
                targetOffset.z = 0f;

                ripperSequence = new RipperSequenceComponent
                {
                    SequenceId = sequence.Id,
                    IsInitialized = true,
                    AttackDirection = SelectAttackDirection(targetOffset)
                };
            }

            float apothem = Mathf.Max(0.01f, attack.DistanceFromSource);
            float sweepLength = apothem * 2f;

            foreach (Entity projectileEntity in projectiles)
            {
                if (!_projectileStash.Has(projectileEntity) ||
                    !_positionStash.Has(projectileEntity) ||
                    !_hitBoxStash.Has(projectileEntity))
                {
                    continue;
                }

                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(projectileEntity);

                Vector3 attackDirection = ripperSequence.AttackDirection;
                for (int turn = 0; turn < projectile.SwingIndex % 4; turn++)
                {
                    attackDirection = new Vector3(
                        -attackDirection.y,
                        attackDirection.x,
                        0f);
                }
                Vector3 railCenterOffset = attackDirection * apothem;

                // The saw advances along its local X axis, then returns along it.
                Vector3 direction = attackDirection;
                bool mirrorY = attackDirection.x < 0f;

                projectile.Direction = direction;

                // Local X faces along the selected diagonal from the player.
                float angle = Mathf.Atan2(
                    attackDirection.y,
                    attackDirection.x) * Mathf.Rad2Deg;
                _rotationStash.Set(projectileEntity,
                    new RotationComponent { Angle = angle });

                _trajectoryStash.Set(projectileEntity,
                    new RipperTrajectoryComponent
                    {
                        SequenceId = projectile.SequenceId,
                        SwingIndex = projectile.SwingIndex,
                        SequencePolicy = projectile.SequencePolicy,
                        AttackDirection = attackDirection,
                        Direction = direction,
                        StartOffset = railCenterOffset,
                        SweepLength = sweepLength,
                        Duration = Mathf.Max(
                            0.01f,
                            attack.Trajectory.Duration *
                            MotionDurationMultiplier),
                        CurrentTime = 0f,
                        ShakeAmplitude = attack.Trajectory.ShakeAmplitude,
                        ShakeFrequency = attack.Trajectory.ShakeFrequency,
                        MirrorY = mirrorY,
                        CompletionReported = false
                    });
            }
        }

        private Vector3 SelectAttackDirection(Vector3 targetOffset)
        {
            Vector2[] configuredVectors = _config.AttackDirections;
            if (configuredVectors == null || configuredVectors.Length == 0)
                return Vector3.right;

            Vector2 targetDirection = targetOffset;
            if (targetDirection.sqrMagnitude <= 0.0001f)
                targetDirection = Vector2.right;
            else
                targetDirection.Normalize();

            Vector3 bestDirection = Vector3.right;
            float bestAlignment = float.MinValue;

            for (int i = 0; i < configuredVectors.Length; i++)
            {
                Vector2 candidate = configuredVectors[i];
                if (candidate.sqrMagnitude <= 0.0001f)
                    continue;

                candidate.Normalize();
                float alignment = Vector2.Dot(
                    targetDirection,
                    candidate);

                if (alignment <= bestAlignment)
                    continue;

                bestAlignment = alignment;
                bestDirection = new Vector3(candidate.x, candidate.y, 0f);
            }

            return bestDirection;
        }
    }
}
