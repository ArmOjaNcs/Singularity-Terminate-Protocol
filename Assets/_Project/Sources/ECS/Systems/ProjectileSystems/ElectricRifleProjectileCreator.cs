using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ElectricRifleProjectileCreator : IProjectileCreator
    {
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<ElectricTrajectoryComponent> _trajectoryStash;

        public ElectricRifleProjectileCreator(World world)
        {
            _attackStash = world.GetStash<AttackComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _trajectoryStash = world.GetStash<ElectricTrajectoryComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            if (!_attackStash.Has(weapon) ||
                projectiles == null ||
                projectiles.Length == 0)
            {
                return;
            }

            AttackComponent attack = _attackStash.Get(weapon);
            Entity target = _targetStash.Has(weapon)
                ? _targetStash.Get(weapon).Target
                : default;

            foreach (Entity projectileEntity in projectiles)
            {
                if (!_projectileStash.Has(projectileEntity))
                    continue;

                Vector3 direction =
                    _projectileStash.Get(projectileEntity).Direction;
                direction.z = 0f;

                float angle = direction.sqrMagnitude > 0.0001f
                    ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                    : 0f;

                _rotationStash.Set(projectileEntity, new RotationComponent
                {
                    Angle = angle
                });

                _trajectoryStash.Set(
                    projectileEntity,
                    new ElectricTrajectoryComponent
                    {
                        Target = target,
                        Amplitude = attack.ZigZagAmplitude,
                        Frequency = attack.ZigZagFrequency,
                        Phase = 0f,
                        ForwardDirection = direction.normalized
                    });
            }
        }
    }
}
