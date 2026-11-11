using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class RocketLauncherProjectileCreator : IProjectileCreator
    {
        private readonly RocketLauncherProjectileConfig _config;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RocketFlightComponent> _flightStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ProjectileViewComponent> _viewStash;

        public RocketLauncherProjectileCreator(
            World world,
            RocketLauncherProjectileConfig config)
        {
            _config = config;
            _attackStash = world.GetStash<AttackComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _flightStash = world.GetStash<RocketFlightComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _viewStash = world.GetStash<ProjectileViewComponent>();
        }

        public void Create(Entity weapon, Entity[] projectiles)
        {
            if (_config == null ||
                !_attackStash.Has(weapon) ||
                !_targetStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                return;
            }

            Entity target = _targetStash.Get(weapon).Target;
            if (!_positionStash.Has(target))
                return;

            AttackComponent attack = _attackStash.Get(weapon);
            Vector3 targetPosition = _positionStash.Get(target).Position;

            foreach (Entity projectileEntity in projectiles)
            {
                if (!_projectileStash.Has(projectileEntity) ||
                    !_positionStash.Has(projectileEntity) ||
                    !_hitBoxStash.Has(projectileEntity))
                {
                    continue;
                }

                Vector3 start = _positionStash.Get(projectileEntity).Position;
                start.z = 0f;
                Vector3 destination = targetPosition;
                destination.z = 0f;
                Vector3 direction = destination - start;
                if (direction.sqrMagnitude <= 0.0001f)
                    continue;
                direction.Normalize();

                float speed = Mathf.Max(0.01f, attack.ProjectileSpeed);
                float flightDistance = Mathf.Max(
                    Vector3.Distance(start, destination),
                    attack.ProjectileFlightDistance +
                    attack.ProjectileFlightDistanceTolerance);

                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(projectileEntity);
                projectile.Direction = direction;
                projectile.CollisionEnabled = false;
                projectile.RicochetChance = 0f;
                projectile.HitPolicy = HitPolicyType.OncePerTarget;
                projectile.Lifetime = Mathf.Max(
                    projectile.Lifetime,
                    flightDistance / speed + _config.ExplosionDuration + 0.5f);

                _flightStash.Set(projectileEntity, new RocketFlightComponent
                {
                    Target = target,
                    Destination = destination,
                    Direction = direction,
                    Speed = speed,
                    MaxDistance = flightDistance,
                    TravelledDistance = 0f,
                    TurnSpeed = Mathf.Max(0f, _config.HomingTurnSpeed),
                    ExplosionDuration = _config.ExplosionDuration,
                    HomingEnabled = attack.HomingEnabled
                });

                _rotationStash.Set(projectileEntity, new RotationComponent
                {
                    Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                });

                ColliderComponent collider = _hitBoxStash.Get(projectileEntity).Collider;
                collider.Type = ColliderType.Circle;
                collider.Center = Vector2.zero;
                _hitBoxStash.Get(projectileEntity).Collider = collider;

                if (_viewStash.Has(projectileEntity) &&
                    _viewStash.Get(projectileEntity).View is RocketLauncherProjectileView view)
                {
                    view.BeginFlight();
                }
            }
        }
    }
}
