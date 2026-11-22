using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class TacticalComplexProjectileCreator : IProjectileCreator
    {
        private readonly TacticalComplexProjectileConfig _config;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<TacticalRocketFlightComponent> _flightStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<RotationComponent> _rotationStash;

        public TacticalComplexProjectileCreator(World world, TacticalComplexProjectileConfig config)
        {
            _config = config;
            _attackStash = world.GetStash<AttackComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _flightStash = world.GetStash<TacticalRocketFlightComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            AttackComponent attack = _attackStash.Get(weapon);
            Vector3 origin = _positionStash.Get(weapon).Position;
            float halfSpread = attack.Pattern.SpreadAngle * 0.5f;
            float launchDistance = Mathf.Max(0f, _config.LaunchDistance);
            float searchRadius = Mathf.Max(0f, attack.TargetSearchRadius);

            for (int i = 0; i < projectiles.Length; i++)
            {
                Entity entity = projectiles[i];
                ref ProjectileComponent rocket = ref _projectileStash.Get(entity);
                float spread = projectiles.Length == 1
                    ? 0f
                    : Mathf.Lerp(-halfSpread, halfSpread, (float)i / (projectiles.Length - 1));
                rocket.Direction = Quaternion.Euler(0f, 0f, spread) * rocket.Direction;
                rocket.CollisionEnabled = false;
                rocket.HitPolicy = HitPolicyType.OncePerTarget;
                rocket.RicochetChance = 0f;
                rocket.RicochetPolicy = RicochetPolicyType.None;

                _positionStash.Get(entity).Position = origin + (rocket.Direction * attack.DistanceFromSource);
                _rotationStash.Set(entity, new RotationComponent
                {
                    Angle = Mathf.Atan2(rocket.Direction.y, rocket.Direction.x) * Mathf.Rad2Deg,
                });
                _flightStash.Set(entity, new TacticalRocketFlightComponent
                {
                    LaunchDistanceRemaining = launchDistance,
                    LaunchSpeed = launchDistance / Mathf.Max(0.01f, _config.LaunchDuration),
                    LockTimeRemaining = Mathf.Max(0f, _config.LockDelay),
                    RetargetDelay = Mathf.Max(0.01f, _config.RetargetDelay),
                    SearchRadiusSquared = searchRadius * searchRadius,
                    ExplosionDuration = Mathf.Max(0.01f, _config.ExplosionDuration),
                });
            }
        }
    }
}
