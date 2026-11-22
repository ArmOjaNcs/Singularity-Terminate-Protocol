using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using Gameplay;
using Gameplay.Spatial;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class GrenadeLauncherProjectileCreator : IProjectileCreator
    {
        private readonly GrenadeLauncherProjectileConfig _config;
        private readonly World _world;
        private readonly SpatialGrid _grid;
        private readonly List<Entity> _candidates = new List<Entity>();
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<DamageableTag> _damageableStash;
        private readonly Stash<EnemyTag> _enemyStash;
        private readonly Stash<PlayerTag> _playerStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<DeathComponent> _deathStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<GrenadeFlightComponent> _flightStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;

        public GrenadeLauncherProjectileCreator(
            World world,
            GrenadeLauncherProjectileConfig config,
            SpatialGrid grid)
        {
            _config = config;
            _world = world;
            _grid = grid;
            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _damageableStash = world.GetStash<DamageableTag>();
            _enemyStash = world.GetStash<EnemyTag>();
            _playerStash = world.GetStash<PlayerTag>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _deathStash = world.GetStash<DeathComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _flightStash = world.GetStash<GrenadeFlightComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
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

            if (_config is ArmageddonProjectileConfig)
            {
                PrepareClusterRelease(weapon, projectiles);
                return;
            }

            Entity target = _targetStash.Get(weapon).Target;
            bool hasTarget = target != default &&
                             _positionStash.Has(target) &&
                             !_deathStash.Has(target);
            AttackComponent attack = _attackStash.Get(weapon);
            Vector3 weaponPosition = _positionStash.Get(weapon).Position;
            Vector3 targetDirection = hasTarget
                ? _positionStash.Get(target).Position - weaponPosition
                : Vector3.zero;
            targetDirection.z = 0f;

            float flightDistance = Mathf.Max(
                0.1f,
                attack.Trajectory.FlightDistance);

            if (targetDirection.sqrMagnitude <= 0.0001f &&
                projectiles != null && projectiles.Length > 0 &&
                _projectileStash.Has(projectiles[0]))
            {
                targetDirection = _projectileStash.Get(projectiles[0]).Direction;
                targetDirection.z = 0f;
            }

            if (targetDirection.sqrMagnitude <= 0.0001f)
                return;

            Vector3 aimDirection = targetDirection.normalized;
            foreach (Entity projectileEntity in projectiles)
            {
                if (!_projectileStash.Has(projectileEntity) ||
                    !_positionStash.Has(projectileEntity) ||
                    !_hitBoxStash.Has(projectileEntity))
                {
                    continue;
                }

                Vector3 start = _positionStash.Get(projectileEntity).Position;
                Vector3 landing = start + aimDirection * flightDistance;
                Launch(projectileEntity, start, landing, _config.ArcOffset);
            }
        }

        private void PrepareClusterRelease(Entity weapon, Entity[] projectiles)
        {
            AttackComponent attack = _attackStash.Get(weapon);
            for (int i = 0; i < projectiles.Length; i++)
            {
                Entity entity = projectiles[i];
                float spread = (i - (projectiles.Length - 1) * 0.5f) * attack.Pattern.SpreadAngle;
                ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
                projectile.Direction = Quaternion.Euler(0f, 0f, spread) * projectile.Direction;
                projectile.CollisionEnabled = false;
                _flightStash.Set(entity, new GrenadeFlightComponent
                {
                    ClusterProjectileScale = attack.ProjectileScale,
                    IsAwaitingLaunch = true
                });
                _rotationStash.Set(entity, default);
            }
        }

        internal bool TryLaunchClusterGrenade(Entity entity)
        {
            ProjectileComponent projectile = _projectileStash.Get(entity);
            Entity weapon = projectile.Weapon;
            if (_world.IsDisposed(weapon) ||
                !_attackStash.Has(weapon) ||
                !_sequenceStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                return false;
            }

            AttackComponent attack = _attackStash.Get(weapon);
            List<Entity> visited = _sequenceStash.Get(weapon).VisitedTargets;
            Vector3 origin = _positionStash.Get(weapon).Position;
            origin.z = 0f;
            Entity target = FindClusterTarget(origin, attack.TargetSearchRadius, projectile.TargetTypes, visited);
            Vector3 direction = projectile.Direction;
            Vector3 landing = origin + direction *
                (attack.DistanceFromSource + Mathf.Max(0.1f, attack.Trajectory.FlightDistance));

            if (target != default)
            {
                visited.Remove(target);
                visited.Add(target);
                landing = _positionStash.Get(target).Position;
                landing.z = 0f;
                Vector3 aim = landing - origin;
                if (aim.sqrMagnitude > 0.0001f)
                    direction = aim.normalized;
            }

            float scale = _flightStash.Get(entity).ClusterProjectileScale;
            Launch(entity, origin + direction * attack.DistanceFromSource,
                landing, _config.ArcOffset, scale);
            return true;
        }

        private Entity FindClusterTarget(
            Vector3 origin, float radius, AttackTargetType[] targetTypes, List<Entity> visited)
        {
            _grid.Query(origin, radius, _candidates);
            Entity best = default;
            int bestVisit = int.MaxValue;
            float bestDistance = float.MaxValue;
            float radiusSquared = radius * radius;

            foreach (Entity candidate in _candidates)
            {
                if (_world.IsDisposed(candidate) ||
                    _deathStash.Has(candidate) ||
                    !_positionStash.Has(candidate) ||
                    !_hitBoxStash.Has(candidate) ||
                    !CommonFunctions.IsValidTarget(
                        candidate, targetTypes, _damageableStash, _enemyStash, _playerStash))
                {
                    continue;
                }

                float distance = ((Vector2)(_positionStash.Get(candidate).Position - origin)).sqrMagnitude;
                int visit = visited.IndexOf(candidate);
                if (distance > radiusSquared || visit > bestVisit ||
                    (visit == bestVisit && distance >= bestDistance))
                {
                    continue;
                }

                best = candidate;
                bestVisit = visit;
                bestDistance = distance;
            }

            return best;
        }

        internal void Launch(
            Entity entity,
            Vector3 start,
            Vector3 landing,
            float arcOffset,
            float clusterProjectileScale = 0f)
        {
            start.z = 0f;
            landing.z = 0f;
            Vector3 span = landing - start;
            float distance = span.magnitude;
            ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
            Vector3 direction = distance > 0.0001f ? span / distance : projectile.Direction;
            Vector3 lateral = new Vector3(-direction.y, direction.x, 0f);
            if (lateral.y < 0f)
                lateral = -lateral;

            float duration = GetFlightDuration(distance, arcOffset, Mathf.Max(0.01f, projectile.Speed));
            projectile.Direction = direction;
            projectile.CollisionEnabled = false;
            projectile.RicochetChance = 0f;
            projectile.HitPolicy = HitPolicyType.OncePerTarget;
            projectile.Lifetime = Mathf.Max(
                projectile.Lifetime, duration + _config.ExplosionDuration + 0.1f);

            _positionStash.Get(entity).Position = start;
            _flightStash.Set(entity, new GrenadeFlightComponent
            {
                StartPosition = start,
                LandingPosition = landing,
                LateralDirection = lateral,
                ArcOffset = arcOffset,
                Duration = duration,
                ExplosionDuration = _config.ExplosionDuration,
                ClusterProjectileScale = clusterProjectileScale
            });

            _rotationStash.Set(entity, new RotationComponent
            {
                Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            });

            ref ColliderComponent collider = ref _hitBoxStash.Get(entity).Collider;
            collider.Type = ColliderType.Circle;
            collider.Center = Vector2.zero;
            collider.Radius = Mathf.Max(0f, collider.Radius);
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
