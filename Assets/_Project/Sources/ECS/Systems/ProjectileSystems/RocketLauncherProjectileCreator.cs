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
        private readonly Stash<DeathComponent> _deathStash;
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
            _deathStash = world.GetStash<DeathComponent>();
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

            AttackComponent attack = _attackStash.Get(weapon);
            Entity target = _targetStash.Get(weapon).Target;

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
                bool hasTarget = target != default &&
                                 _positionStash.Has(target) &&
                                 !_deathStash.Has(target);
                Vector3 destination = start;
                Vector3 direction = Vector3.zero;
                if (hasTarget)
                {
                    destination = _positionStash.Get(target).Position;
                    destination.z = 0f;
                    direction = destination - start;
                    hasTarget = direction.sqrMagnitude > 0.0001f;
                }

                if (!hasTarget)
                {
                    direction = _projectileStash.Get(projectileEntity).Direction;
                    direction.z = 0f;
                }

                if (direction.sqrMagnitude <= 0.0001f)
                    continue;
                direction.Normalize();

                float speed = Mathf.Max(0.01f, attack.ProjectileSpeed);
                float configuredDistance = attack.Trajectory.FlightDistance +
                                           attack.Trajectory.FlightDistanceTolerance;
                float flightDistance = hasTarget
                    ? Mathf.Max(Vector3.Distance(start, destination), configuredDistance)
                    : Mathf.Max(0.1f, configuredDistance);
                if (!hasTarget)
                    destination = start + direction * flightDistance;

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
