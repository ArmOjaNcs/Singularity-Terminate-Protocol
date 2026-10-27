using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class StraightProjectileCreator : IProjectileCreator
    {
        private readonly StraightProjectileConfig _config;

        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<StraightTrajectoryComponent> _trajectoryStash;
        private readonly Stash<RotationComponent> _rotationStash;

        public StraightProjectileCreator(
            World world,
            StraightProjectileConfig config)
        {
            _config = config;

            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _trajectoryStash = world.GetStash<StraightTrajectoryComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
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

            if (projectiles == null || projectiles.Length == 0)
                return;

            Entity firstProjectile = projectiles[0];

            if (!_projectileStash.Has(firstProjectile))
                return;

            Vector3 baseDirection =
                _projectileStash.Get(firstProjectile).Direction;

            baseDirection.z = 0f;

            if (baseDirection.sqrMagnitude <= 0.0001f)
                return;

            baseDirection.Normalize();

            float spreadAngle = attack.ShotSpreadAngle;
            int projectileCount = projectiles.Length;

            for (int i = 0; i < projectileCount; i++)
            {
                Entity projectile = projectiles[i];

                if (!_projectileStash.Has(projectile) ||
                    !_positionStash.Has(projectile))
                {
                    continue;
                }

                float angle = GetSpreadAngle(
                    i,
                    projectileCount,
                    spreadAngle);

                Vector3 direction = RotateDirection(baseDirection, angle);

                ref ProjectileComponent projectileComponent =
                    ref _projectileStash.Get(projectile);

                projectileComponent.Direction = direction;

                _trajectoryStash.Set(projectile, new StraightTrajectoryComponent());

                _rotationStash.Set(
                    projectile,
                    new RotationComponent
                    {
                        Angle = Mathf.Atan2(direction.y, direction.x) *
                                Mathf.Rad2Deg
                    });

            }
        }

        private float GetSpreadAngle(
            int projectileIndex,
            int projectileCount,
            float spreadAngle)
        {
            if (projectileCount <= 1)
                return 0f;

            float halfAngle = spreadAngle * 0.5f;

            float normalizedIndex =
                projectileIndex / (float)(projectileCount - 1);

            return Mathf.Lerp(-halfAngle, halfAngle, normalizedIndex);
        }

        private Vector3 RotateDirection(Vector3 direction, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector3(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos,
                0f).normalized;
        }
    }
}
