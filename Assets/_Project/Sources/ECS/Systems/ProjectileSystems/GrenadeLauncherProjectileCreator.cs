using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class GrenadeLauncherProjectileCreator : IProjectileCreator
    {
        private readonly GrenadeLauncherProjectileConfig _config;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<GrenadeFlightComponent> _flightStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ProjectileViewComponent> _viewStash;

        public GrenadeLauncherProjectileCreator(
            World world,
            GrenadeLauncherProjectileConfig config)
        {
            _config = config;
            _attackStash = world.GetStash<AttackComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _flightStash = world.GetStash<GrenadeFlightComponent>();
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
            Vector3 weaponPosition = _positionStash.Get(weapon).Position;
            Vector3 targetPosition = _positionStash.Get(target).Position;
            Vector3 aimDirection = targetPosition - weaponPosition;
            aimDirection.z = 0f;
            if (aimDirection.sqrMagnitude <= 0.0001f)
                return;
            aimDirection.Normalize();

            float flightDistance = Mathf.Max(
                0.1f,
                attack.ProjectileFlightDistance);
            float flightDuration = GetFlightDuration(
                flightDistance,
                _config.ArcOffset,
                Mathf.Max(0.01f, attack.ProjectileSpeed));

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
                Vector3 landing = start + aimDirection * flightDistance;

                // Keep the canonical arc on the same side of the X axis so it
                // never becomes an upside-down rainbow when aiming left.
                Vector3 lateral = new Vector3(
                    -aimDirection.y,
                    aimDirection.x,
                    0f);
                if (lateral.y < 0f)
                    lateral = -lateral;

                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(projectileEntity);
                projectile.CollisionEnabled = false;
                projectile.RicochetChance = 0f;
                projectile.HitPolicy = HitPolicyType.OncePerTarget;
                projectile.Lifetime = Mathf.Max(
                    projectile.Lifetime,
                    flightDuration + _config.ExplosionDuration + 0.1f);

                _flightStash.Set(projectileEntity, new GrenadeFlightComponent
                {
                    StartPosition = start,
                    LandingPosition = landing,
                    LateralDirection = lateral,
                    ArcOffset = _config.ArcOffset,
                    Duration = flightDuration,
                    Elapsed = 0f,
                    ExplosionDuration = _config.ExplosionDuration
                });

                _rotationStash.Set(projectileEntity, new RotationComponent
                {
                    Angle = Mathf.Atan2(landing.y - start.y, landing.x - start.x) * Mathf.Rad2Deg
                });

                ColliderComponent collider = _hitBoxStash.Get(projectileEntity).Collider;
                collider.Type = ColliderType.Circle;
                collider.Center = Vector2.zero;
                collider.Radius = Mathf.Max(0f, collider.Radius);
                _hitBoxStash.Get(projectileEntity).Collider = collider;

                if (_viewStash.Has(projectileEntity) &&
                    _viewStash.Get(projectileEntity).View is Gameplay.View.GrenadeLauncherProjectileView view)
                    view.BeginFlight();
            }
        }

        private static float GetFlightDuration(
            float forwardDistance,
            float arcOffset,
            float speed)
        {
            const int Samples = 24;
            Vector2 previous = Vector2.zero;
            float arcLength = 0f;

            for (int i = 1; i <= Samples; i++)
            {
                float progress = i / (float)Samples;
                Vector2 point = new Vector2(
                    forwardDistance * progress,
                    4f * progress * (1f - progress) * arcOffset);
                arcLength += Vector2.Distance(previous, point);
                previous = point;
            }

            return Mathf.Max(0.05f, arcLength / speed);
        }
    }
}
