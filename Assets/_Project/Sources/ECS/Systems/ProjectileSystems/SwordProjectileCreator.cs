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
        private readonly ArcProjectileConfig _config;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<SwordThrustComponent> _thrustStash;
        private readonly Stash<SwordSlashComponent> _swordSlashStash;

        public SwordProjectileCreator(World world, ArcProjectileConfig config)
        {
            _config = config;
            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _thrustStash = world.GetStash<SwordThrustComponent>();
            _swordSlashStash = world.GetStash<SwordSlashComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            if (_config == null || !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon))
                return;

            AttackComponent attack = _attackStash.Get(weapon);
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            Vector3 forward = projectiles.Length > 0 &&
                              _projectileStash.Has(projectiles[0])
                ? _projectileStash.Get(projectiles[0]).Direction
                : Vector3.right;
            forward.z = 0f;
            forward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector3.right;

            Vector3 lateral = new Vector3(-forward.y, forward.x, 0f);
            Vector3 playerPosition = _positionStash.Get(weapon).Position;
            Vector3 patternOrigin = sequence.SwordRicochetCycle
                ? sequence.SwordRicochetOrigin
                : Vector3.zero;
            float step = Mathf.Max(0.1f, attack.SwordThrustDistance);
            float width = Mathf.Max(0.1f, attack.SwordZigZagWidth);
            Vector3 initialOffset = sequence.SwordRicochetCycle
                ? Vector3.zero
                : forward * attack.DistanceFromSource;
            Vector3 firstCorner = patternOrigin + initialOffset;
            Vector3[] starts = new Vector3[projectiles.Length];
            Vector3[] ends = new Vector3[projectiles.Length];
            for (int index = 0; index < projectiles.Length; index++)
            {
                starts[index] = index == 0 ? firstCorner : ends[index - 1];
                float lateralStep = index == 0
                    ? -width
                    : (index % 2 == 1 ? 2f * width : -2f * width);
                ends[index] = starts[index] + forward * step +
                              lateral * lateralStep;
            }

            for (int index = 0; index < projectiles.Length; index++)
            {
                Entity projectileEntity = projectiles[index];
                if (!_projectileStash.Has(projectileEntity) ||
                    !_positionStash.Has(projectileEntity) ||
                    !_hitBoxStash.Has(projectileEntity))
                    continue;

                Vector3 segment = ends[index] - starts[index];
                Vector3 direction = segment.normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) *
                              Mathf.Rad2Deg;
                float startDelay = attack.ShotInterval * index;

                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(projectileEntity);
                projectile.Direction = direction;
                projectile.SwingIndex = index;
                projectile.CurrentLifetime = -startDelay;
                if (startDelay > 0f)
                    projectile.CollisionEnabled = false;

                bool followOwner = !sequence.SwordRicochetCycle;
                Vector3 startPosition = followOwner
                    ? playerPosition + starts[index]
                    : starts[index];
                Vector3 endPosition = followOwner
                    ? playerPosition + ends[index]
                    : ends[index];
                _positionStash.Get(projectileEntity).Position = startPosition;
                _rotationStash.Set(projectileEntity,
                    new RotationComponent { Angle = angle });
                _thrustStash.Set(projectileEntity,
                    new SwordThrustComponent
                    {
                        Owner = weapon,
                        StartPosition = followOwner ? starts[index] : startPosition,
                        EndPosition = followOwner ? ends[index] : endPosition,
                        FollowOwner = followOwner,
                        Duration = Mathf.Max(0.01f, attack.Trajectory.Duration),
                        CurrentTime = -startDelay
                    });

                ref HitBoxComponent hitBox =
                    ref _hitBoxStash.Get(projectileEntity);
                ColliderComponent collider = hitBox.Collider;
                collider.Angle = angle;
                hitBox.Collider = collider;

                _swordSlashStash.Set(projectileEntity,
                    new SwordSlashComponent
                    {
                        SequenceId = projectile.SequenceId,
                        SwingIndex = index,
                        MirrorY = forward.x < 0f
                    });
            }
        }
    }
}
