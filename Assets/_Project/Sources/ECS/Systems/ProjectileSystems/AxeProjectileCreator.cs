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
        public const int SectorCount = 4;

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
                    int preferredSector = GetCardinalSector(targetDirection);
                    int sector = FindNextFreeSector(
                        preferredSector,
                        sequence.UsedSectorMask);

                    if (sector < 0)
                        continue;

                    sequence.AssignedSectorCycle = cycleIndex;
                    sequence.AssignedSectorIndex = sector;
                    sequence.UsedSectorMask |= 1 << sector;
                }

                Vector3 direction = GetCardinalDirection(
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

        private static int GetCardinalSector(Vector3 targetDirection)
        {
            if (targetDirection.sqrMagnitude <= 0.0001f)
                return 0;

            if (Mathf.Abs(targetDirection.x) >= Mathf.Abs(targetDirection.y))
                return targetDirection.x < 0f ? 2 : 0;

            return targetDirection.y < 0f ? 3 : 1;
        }

        private static int FindNextFreeSector(int preferredSector, int usedMask)
        {
            for (int offset = 0; offset < SectorCount; offset++)
            {
                int sector = (preferredSector + offset) % SectorCount;
                if ((usedMask & (1 << sector)) == 0)
                    return sector;
            }

            return -1;
        }

        private static Vector3 GetCardinalDirection(int sector)
        {
            return sector switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                2 => Vector3.left,
                3 => Vector3.down,
                _ => Vector3.right
            };
        }
    }
}
