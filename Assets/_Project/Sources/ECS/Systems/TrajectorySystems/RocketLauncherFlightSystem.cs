using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class RocketLauncherFlightSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Filter _enemyFilter;
        private Filter _playerFilter;
        private Stash<RocketFlightComponent> _flightStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<AreaExpansionComponent> _areaStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<ProjectileViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<RocketFlightComponent>()
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<HitBoxComponent>()
                .Build();

            _enemyFilter = World.Filter
                .With<EnemyTag>()
                .Without<DeathComponent>()
                .Build();
            _playerFilter = World.Filter
                .With<PlayerTag>()
                .Without<DeathComponent>()
                .Build();

            _flightStash = World.GetStash<RocketFlightComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _areaStash = World.GetStash<AreaExpansionComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _viewStash = World.GetStash<ProjectileViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref RocketFlightComponent flight = ref _flightStash.Get(entity);
                Vector3 position = _positionStash.Get(entity).Position;
                position.z = 0f;

                bool targetIsAlive = IsTargetAlive(flight.Target);
                Vector3 destination = flight.Destination;
                if (flight.HomingEnabled && targetIsAlive)
                {
                    destination = _positionStash.Get(flight.Target).Position;
                    destination.z = 0f;
                    Vector3 toTarget = destination - position;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        flight.Direction = Vector3.RotateTowards(
                            flight.Direction,
                            toTarget.normalized,
                            flight.TurnSpeed * Mathf.Deg2Rad * deltaTime,
                            0f).normalized;
                    }
                }

                float remainingRange = flight.MaxDistance - flight.TravelledDistance;
                float step = Mathf.Min(
                    flight.Speed * Mathf.Max(0f, deltaTime),
                    Mathf.Max(0f, remainingRange));
                bool reachedTarget =
                    (!flight.HomingEnabled || targetIsAlive) &&
                    Vector3.Distance(position, destination) <= step;
                bool exhaustedRange = remainingRange <= step;

                if (reachedTarget || exhaustedRange)
                {
                    if (reachedTarget)
                        position = destination;
                    Detonate(entity, position, flight.ExplosionDuration);
                    continue;
                }

                position += flight.Direction * step;
                flight.TravelledDistance += step;
                _positionStash.Get(entity).Position = position;

                ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
                projectile.Direction = flight.Direction;
                float angle = Mathf.Atan2(
                    flight.Direction.y,
                    flight.Direction.x) * Mathf.Rad2Deg;
                _rotationStash.Get(entity).Angle = angle;

                ColliderComponent collider = _hitBoxStash.Get(entity).Collider;
                collider.Angle = angle;
                _hitBoxStash.Get(entity).Collider = collider;
            }
        }

        private bool IsTargetAlive(Entity target)
        {
            if (target == default ||
                World.IsDisposed(target) ||
                !_positionStash.Has(target))
            {
                return false;
            }

            return _enemyFilter.Has(target) || _playerFilter.Has(target);
        }

        private void Detonate(Entity entity, Vector3 position, float duration)
        {
            _positionStash.Get(entity).Position = position;
            ColliderComponent baseCollider = _hitBoxStash.Get(entity).Collider;
            _flightStash.Remove(entity);
            _areaStash.Set(entity, new AreaExpansionComponent
            {
                BaseCollider = baseCollider,
                Duration = Mathf.Max(0.01f, duration),
                Elapsed = 0f,
                ReachedMaximum = false
            });

            ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
            projectile.CollisionEnabled = true;
            projectile.HitPolicy = HitPolicyType.OncePerTarget;

            if (_viewStash.Has(entity) &&
                _viewStash.Get(entity).View is RocketLauncherProjectileView view)
            {
                view.BeginExplosion();
            }
        }

        public void Dispose() { }
    }
}
