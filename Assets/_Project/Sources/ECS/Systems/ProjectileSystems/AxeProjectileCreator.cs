using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class AxeProjectileCreator : IProjectileCreator
    {
        private readonly ArcProjectileConfig _config;

        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ProjectileVisualFlipComponent> _visualFlipStash;

        public AxeProjectileCreator(
            World world,
            ArcProjectileConfig config)
        {
            _config = config;

            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _arcTrajectoryStash = world.GetStash<ArcTrajectoryComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _visualFlipStash = world.GetStash<ProjectileVisualFlipComponent>();
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

            if (_config.WeaponType == WeaponType.BladeGuard)
            {
                CreateGuardRing(weapon, projectiles, attack, sequence.Id);
                return;
            }

            foreach (Entity projectile in projectiles)
            {
                if (!_projectileStash.Has(projectile) ||
                    !_positionStash.Has(projectile))
                {
                    continue;
                }

                Vector3 targetDirection =
                    _projectileStash.Get(projectile).Direction;
                targetDirection.z = 0f;

                if (!sequence.HasAxeSweepDirection)
                {
                    sequence.AxeSweepClockwise = targetDirection.x >= 0f;
                    sequence.HasAxeSweepDirection = true;
                }

                int cycleIndex = _projectileStash.Get(projectile).SwingIndex;
                if (sequence.AssignedSectorCycle != cycleIndex)
                {
                    int preferredSector = CardinalSectors.FromDirection(targetDirection);
                    int sector = CardinalSectors.FindAvailable(
                        preferredSector,
                        sequence.UsedSectorMask);

                    if (sector < 0)
                        continue;

                    sequence.AssignedSectorCycle = cycleIndex;
                    sequence.AssignedSectorIndex = sector;
                    sequence.UsedSectorMask |= 1 << sector;
                }

                Vector3 direction = CardinalSectors.ToDirection(
                    sequence.AssignedSectorIndex);

                if (_positionStash.Has(weapon))
                {
                    ref PositionComponent projectilePosition =
                        ref _positionStash.Get(projectile);
                    projectilePosition.Position =
                        _positionStash.Get(weapon).Position +
                        direction * attack.DistanceFromSource;
                }

                ref ProjectileComponent projectileComponent =
                    ref _projectileStash.Get(projectile);
                projectileComponent.Direction = direction;

                _visualFlipStash.Set(projectile, new ProjectileVisualFlipComponent
                {
                    FlipY = !sequence.AxeSweepClockwise
                });

                Vector3 colliderOffset = _config.ColliderOffset;

                _rotationStash.Set(projectile, new RotationComponent
                {
                    Angle = 0f
                });

                bool reverseTrajectoryAngles =
                    (direction.x < 0f) == sequence.AxeSweepClockwise;
                float startAngle = reverseTrajectoryAngles
                    ? _config.EndAngle
                    : _config.StartAngle;
                float endAngle = reverseTrajectoryAngles
                    ? _config.StartAngle
                    : _config.EndAngle;

                _arcTrajectoryStash.Set(
                    projectile,
                    new ArcTrajectoryComponent
                    {
                        Direction = direction,
                        ColliderOffset = colliderOffset,
                        DistanceFromSource = attack.DistanceFromSource,
                        Radius = _config.TrajectoryRadius,
                        StartAngle = startAngle,
                        EndAngle = endAngle,
                        StartRotationAngle = startAngle,
                        EndRotationAngle = endAngle,
                        Duration = attack.Trajectory.Duration,
                        CurrentTime = 0f,
                        BaseColliderCenter =
                            _hitBoxStash.Get(projectile).Collider.Center
                    });
            }
        }

        private void CreateGuardRing(
            Entity weapon, Entity[] projectiles, in AttackComponent attack, int sequenceId)
        {
            float sweep = 360f * Mathf.Max(0f, attack.Trajectory.Revolutions);
            if ((sequenceId & 1) == 0)
                sweep = -sweep;

            float duration = Mathf.Max(0.01f, attack.Trajectory.Duration);
            Vector3 center = _positionStash.Get(weapon).Position;
            for (int i = 0; i < projectiles.Length; i++)
            {
                Entity projectile = projectiles[i];
                float start = _config.StartAngle + 360f * i / projectiles.Length;
                float end = start + sweep;
                Vector3 radial = Quaternion.Euler(0f, 0f, start) * Vector3.right;
                _positionStash.Get(projectile).Position = center + radial * _config.TrajectoryRadius;
                _rotationStash.Set(projectile, new RotationComponent { Angle = start });
                _projectileStash.Get(projectile).Lifetime = duration;
                _visualFlipStash.Set(projectile, new ProjectileVisualFlipComponent
                {
                    FlipY = sweep > 0f
                });

                ref ColliderComponent collider = ref _hitBoxStash.Get(projectile).Collider;
                collider.Angle = start;
                _arcTrajectoryStash.Set(projectile, new ArcTrajectoryComponent
                {
                    Anchor = ArcTrajectoryAnchor.Weapon,
                    Direction = Vector3.right,
                    Radius = _config.TrajectoryRadius,
                    StartAngle = start,
                    EndAngle = end,
                    StartRotationAngle = start,
                    EndRotationAngle = end,
                    Duration = duration,
                    BaseColliderCenter = collider.Center,
                    ColliderOffset = _config.ColliderOffset
                });
            }
        }
    }
}
