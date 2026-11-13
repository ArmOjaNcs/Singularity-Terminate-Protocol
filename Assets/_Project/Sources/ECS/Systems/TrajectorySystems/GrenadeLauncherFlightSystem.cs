using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class GrenadeLauncherFlightSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Stash<GrenadeFlightComponent> _flightStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<AreaExpansionComponent> _areaStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<ProjectileViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<GrenadeFlightComponent>()
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<HitBoxComponent>()
                .Build();

            _flightStash = World.GetStash<GrenadeFlightComponent>();
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
                if (_projectileStash.Get(entity).CurrentLifetime < 0f)
                    continue;

                ref GrenadeFlightComponent flight = ref _flightStash.Get(entity);
                flight.Elapsed += deltaTime;
                float progress = flight.Duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(flight.Elapsed / flight.Duration);

                Vector3 span = flight.LandingPosition - flight.StartPosition;
                Vector3 position = Vector3.Lerp(
                    flight.StartPosition,
                    flight.LandingPosition,
                    progress) +
                    flight.LateralDirection *
                    (4f * progress * (1f - progress) * flight.ArcOffset);

                Vector3 tangent = span + flight.LateralDirection *
                    (4f * (1f - 2f * progress) * flight.ArcOffset);
                if (tangent.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                    _rotationStash.Get(entity).Angle = angle;
                    ref ColliderComponent collider = ref _hitBoxStash.Get(entity).Collider;
                    collider.Angle = angle;
                }

                _positionStash.Get(entity).Position = position;
                if (progress < 1f)
                    continue;

                float explosionDuration = flight.ExplosionDuration;
                ColliderComponent baseCollider = _hitBoxStash.Get(entity).Collider;
                _flightStash.Remove(entity);
                _areaStash.Set(entity, new AreaExpansionComponent
                {
                    BaseCollider = baseCollider,
                    Duration = Mathf.Max(0.01f, explosionDuration),
                    Elapsed = 0f,
                    ReachedMaximum = false
                });

                ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
                projectile.CollisionEnabled = true;
                projectile.HitPolicy = HitPolicyType.OncePerTarget;

                if (_viewStash.Has(entity) &&
                    _viewStash.Get(entity).View is GrenadeLauncherProjectileView view)
                    view.BeginExplosion();
            }
        }

        public void Dispose() { }
    }
}
