using System.Collections.Generic;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class TacticalRocketFlightSystem : ISystem
    {
        private readonly HashSet<Entity> _assignedTargets = new HashSet<Entity>();
        private Filter _rockets;
        private Filter _enemies;
        private Stash<TacticalRocketFlightComponent> _flightStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HealthComponent> _healthStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<AreaExpansionComponent> _areaStash;
        private Stash<ProjectileViewComponent> _viewStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _rockets = World.Filter.With<TacticalRocketFlightComponent>().With<ProjectileComponent>()
                .With<PositionComponent>().With<RotationComponent>().With<HitBoxComponent>().Build();
            _enemies = World.Filter.With<EnemyTag>().With<HealthComponent>().With<StatsComponent>()
                .With<PositionComponent>().With<HitBoxComponent>().Without<DeathComponent>().Build();

            _flightStash = World.GetStash<TacticalRocketFlightComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _healthStash = World.GetStash<HealthComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _areaStash = World.GetStash<AreaExpansionComponent>();
            _viewStash = World.GetStash<ProjectileViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _rockets)
            {
                ref ProjectileComponent rocket = ref _projectileStash.Get(entity);
                if (rocket.CleanupRequested || rocket.CurrentLifetime < 0f)
                    continue;

                UpdateRocket(entity, ref rocket, deltaTime);
            }
        }

        public void Dispose()
        {
        }

        private void UpdateRocket(Entity entity, ref ProjectileComponent rocket, float time)
        {
            ref TacticalRocketFlightComponent flight = ref _flightStash.Get(entity);
            ref Vector3 position = ref _positionStash.Get(entity).Position;
            if (flight.LaunchDistanceRemaining > 0f)
            {
                float launchTime = Mathf.Min(time, flight.LaunchDistanceRemaining / flight.LaunchSpeed);
                float distance = launchTime * flight.LaunchSpeed;
                position += rocket.Direction * distance;
                flight.LaunchDistanceRemaining = Mathf.Max(0f, flight.LaunchDistanceRemaining - distance);
                time -= launchTime;
                if (time <= 0f)
                    return;
            }

            if (flight.Target != default && !IsAlive(flight.Target))
            {
                flight.Target = default;
                flight.LockTimeRemaining = flight.RetargetDelay;
                return;
            }

            if (flight.Target == default)
            {
                if (flight.LockTimeRemaining > time)
                {
                    flight.LockTimeRemaining -= time;
                    return;
                }

                time -= flight.LockTimeRemaining;
                flight.LockTimeRemaining = 0f;
                flight.Target = FindTarget(entity, rocket, position, flight.SearchRadiusSquared);
                if (flight.Target == default)
                {
                    flight.LockTimeRemaining = flight.RetargetDelay;
                    return;
                }
            }

            Vector3 destination = ColliderGeometry.GetColliderWorldCenter(
                _positionStash.Get(flight.Target).Position, _hitBoxStash.Get(flight.Target).Collider);
            destination.z = position.z;
            Vector3 offset = destination - position;
            float step = Mathf.Max(0.01f, rocket.Speed) * time;
            if (offset.sqrMagnitude > 0f)
            {
                rocket.Direction = offset.normalized;
                _rotationStash.Get(entity).Angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            }

            if (offset.sqrMagnitude <= step * step)
            {
                position = destination;
                Detonate(entity, ref rocket, flight.ExplosionDuration);
                return;
            }

            position += rocket.Direction * step;
        }

        private Entity FindTarget(Entity entity, in ProjectileComponent rocket, Vector3 position, float radiusSquared)
        {
            _assignedTargets.Clear();
            foreach (Entity other in _rockets)
            {
                if (other == entity || !_flightStash.Has(other))
                    continue;

                ref ProjectileComponent sibling = ref _projectileStash.Get(other);
                Entity target = _flightStash.Get(other).Target;
                if (target != default && !sibling.CleanupRequested &&
                    sibling.Weapon == rocket.Weapon && sibling.SequenceId == rocket.SequenceId &&
                    sibling.CycleIndex == rocket.CycleIndex)
                {
                    _assignedTargets.Add(target);
                }
            }

            Entity best = default;
            bool bestAssigned = true;
            float bestHealth = float.NegativeInfinity;
            float bestDistance = float.PositiveInfinity;
            foreach (Entity enemy in _enemies)
            {
                if (_healthStash.Get(enemy).Current <= 0f)
                    continue;

                Vector2 offset = _positionStash.Get(enemy).Position - position;
                float distance = offset.sqrMagnitude;
                if (distance > radiusSquared)
                    continue;

                // Spread a salvo across available targets before doubling up.
                bool assigned = _assignedTargets.Contains(enemy);
                float maxHealth = _statsStash.Get(enemy).MaxHealth;
                if (assigned && !bestAssigned)
                    continue;

                if (assigned == bestAssigned &&
                    (maxHealth < bestHealth || (maxHealth == bestHealth && distance >= bestDistance)))
                    continue;

                best = enemy;
                bestAssigned = assigned;
                bestHealth = maxHealth;
                bestDistance = distance;
            }

            return best;
        }

        private bool IsAlive(Entity target)
        {
            return !World.IsDisposed(target) && _enemies.Has(target) &&
                   _healthStash.Get(target).Current > 0f;
        }

        private void Detonate(Entity entity, ref ProjectileComponent rocket, float duration)
        {
            _flightStash.Remove(entity);
            _areaStash.Set(entity, new AreaExpansionComponent
            {
                BaseCollider = _hitBoxStash.Get(entity).Collider,
                Duration = duration,
            });
            rocket.CollisionEnabled = true;
            if (rocket.Lifetime > 0f)
                rocket.Lifetime = Mathf.Max(rocket.Lifetime, rocket.CurrentLifetime + duration + 0.1f);

            if (_viewStash.Has(entity) && _viewStash.Get(entity).View is ExplosiveProjectileView view)
                view.BeginExplosion();
        }

    }
}
