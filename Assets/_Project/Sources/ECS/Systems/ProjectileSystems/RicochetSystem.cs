using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class RicochetSystem : ISystem
    {
        private const float MinimumHitSuppressionDuration = 0.2f;
        private const float HitSuppressionDistance = 2.5f;
        private const float MinimumRepeatTargetDetourDistance = 4f;
        private const float RepeatTargetDetourClearance = 1.5f;
        private const float DetourDirectionVariance = 45f;

        private RicochetTargetSelector _targetSelector;
        private readonly List<Entity> _candidates = new List<Entity>();

        private Filter _filter;
        private Filter _livingEnemyFilter;
        private Stash<RicochetRequest> _requestStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<AttackSequenceComponent> _sequenceStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private Stash<ElectricTrajectoryComponent> _electricTrajectoryStash;

        public RicochetSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        private readonly SpatialGrid _grid;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter.With<RicochetRequest>().Build();
            _livingEnemyFilter = World.Filter.With<EnemyTag>()
                .Without<DeathComponent>().Build();
            _requestStash = World.GetStash<RicochetRequest>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _sequenceStash = World.GetStash<AttackSequenceComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _arcTrajectoryStash = World.GetStash<ArcTrajectoryComponent>();
            _electricTrajectoryStash = World.GetStash<ElectricTrajectoryComponent>();
            _targetSelector = new RicochetTargetSelector(World, _grid);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _filter)
            {
                RicochetRequest request = _requestStash.Get(requestEntity);

                if (!_projectileStash.Has(request.Projectile))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(request.Projectile);

                if (projectile.CleanupRequested ||
                    projectile.TargetType != AttackTargetType.Enemy ||
                    projectile.RicochetChance <= 0f ||
                    projectile.TargetSearchRadius <= 0f)
                {
                    projectile.RicochetPending = false;
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                float chance = Mathf.Clamp01(projectile.RicochetChance / 100f);
                if (Random.value > chance)
                {
                    projectile.RicochetPending = false;
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (request.SequencePolicy ==
                    ProjectileSequencePolicy.ComboOnTarget)
                {
                    TryQueueComboRicochet(request, ref projectile);
                }
                else
                {
                    TryApplyProjectileRicochet(request, ref projectile);
                }

                projectile.RicochetPending = false;

                World.RemoveEntity(requestEntity);
            }
        }

        private void TryApplyProjectileRicochet(
            RicochetRequest request,
            ref ProjectileComponent projectile)
        {
            bool isElectricProjectile =
                _electricTrajectoryStash.Has(request.Projectile);
            if (!isElectricProjectile &&
                projectile.RicochetsCount >= projectile.MaxRicochets)
                return;

            bool selected = _targetSelector.TrySelectNearest(
                    request.Projectile,
                    request.HitTarget,
                    request.HitPosition,
                    projectile.TargetSearchRadius,
                    _candidates,
                    null,
                    false,
                    out Entity target,
                    out Vector3 direction);

            if (!selected && projectile.AllowRicochetRepeatTargets)
            {
                selected = _targetSelector.TrySelectNearest(
                    request.Projectile,
                    request.HitTarget,
                    _positionStash.Has(request.Projectile)
                        ? _positionStash.Get(request.Projectile).Position
                        : request.HitPosition,
                    projectile.TargetSearchRadius,
                    _candidates,
                    null,
                    true,
                    out target,
                    out direction);
            }

            // ElectricRifle keeps one projectile for the whole attack cycle.
            // If the only living target is the one just hit, the selector can
            // reject it (for example when projectile and target centers match).
            // Keep that target and send the projectile through a short detour.
            if (!selected && isElectricProjectile &&
                IsLivingEnemy(request.HitTarget))
            {
                target = request.HitTarget;
                direction = _positionStash.Get(target).Position -
                            _positionStash.Get(request.Projectile).Position;
                direction.z = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                    direction = projectile.Direction;
                if (direction.sqrMagnitude <= 0.0001f)
                    direction = Vector3.right;
                direction.Normalize();
                selected = true;
            }

            if (!selected)
            {
                return;
            }

            Vector3 incomingDirection = projectile.Direction;
            incomingDirection.z = 0f;
            if (incomingDirection.sqrMagnitude <= 0.0001f)
                incomingDirection = -direction;
            else
                incomingDirection.Normalize();

            projectile.Direction = direction;
            projectile.RicochetsCount++;
            projectile.RicochetLeg++;
            projectile.TemporarilyIgnoredTarget = request.HitTarget;
            projectile.HitSuppressionUntilTime =
                projectile.CurrentLifetime + Mathf.Max(
                    MinimumHitSuppressionDuration,
                    HitSuppressionDistance / Mathf.Max(0.01f, projectile.Speed));

            if (_electricTrajectoryStash.Has(request.Projectile))
            {
                ref ElectricTrajectoryComponent trajectory =
                    ref _electricTrajectoryStash.Get(request.Projectile);
                trajectory.Target = target;
                trajectory.ForwardDirection = direction;
                trajectory.IsDetouring = target == request.HitTarget;

                if (trajectory.IsDetouring)
                {
                    Vector3 projectilePosition =
                        _positionStash.Get(request.Projectile).Position;
                    Vector3 targetPosition = _positionStash.Get(target).Position;
                    float targetExtent = _hitBoxStash.Has(target)
                        ? GetColliderBoundingRadius(
                            _hitBoxStash.Get(target).Collider)
                        : 0f;
                    float projectileExtent = _hitBoxStash.Has(request.Projectile)
                        ? GetColliderBoundingRadius(
                            _hitBoxStash.Get(request.Projectile).Collider)
                        : 0f;
                    Vector3 outwardDirection = projectilePosition - targetPosition;
                    outwardDirection.z = 0f;
                    if (outwardDirection.sqrMagnitude <= 0.0001f)
                    {
                        outwardDirection = -projectile.Direction;
                        outwardDirection.z = 0f;
                    }
                    if (outwardDirection.sqrMagnitude <= 0.0001f)
                        outwardDirection = Vector3.right;
                    outwardDirection.Normalize();

                    float currentDistance =
                        Vector3.Distance(projectilePosition, targetPosition);
                    float requiredClearance = Mathf.Max(
                        0f,
                        targetExtent + projectileExtent +
                        RepeatTargetDetourClearance - currentDistance);
                    float minimumTravelToClear = requiredClearance /
                        Mathf.Cos(DetourDirectionVariance * Mathf.Deg2Rad);
                    float detourDistance = Mathf.Max(
                        MinimumRepeatTargetDetourDistance,
                        minimumTravelToClear);

                    float angle = Random.Range(
                        -DetourDirectionVariance,
                        DetourDirectionVariance);
                    Vector3 detourDirection =
                        Quaternion.Euler(0f, 0f, angle) * outwardDirection;
                    trajectory.ForwardDirection = detourDirection;
                    trajectory.RicochetWaypoint = projectilePosition +
                        detourDirection * detourDistance;
                }
            }

            if (_arcTrajectoryStash.Has(request.Projectile))
            {
                ref ArcTrajectoryComponent trajectory =
                    ref _arcTrajectoryStash.Get(request.Projectile);
                trajectory.Direction = direction;
                trajectory.CurrentTime = 0f;
            }
        }

        private bool IsLivingEnemy(Entity target)
        {
            return target != default && !World.IsDisposed(target) &&
                   _livingEnemyFilter.Has(target) && _positionStash.Has(target);
        }

        private static float GetColliderBoundingRadius(
            ColliderComponent collider)
        {
            float radius = collider.Type switch
            {
                ColliderType.Circle => Mathf.Max(0f, collider.Radius),
                ColliderType.Box => collider.Size.magnitude * 0.5f,
                ColliderType.Capsule => Mathf.Max(
                    Mathf.Max(0f, collider.Length) * 0.5f,
                    Mathf.Max(0f, collider.Radius)),
                ColliderType.Sector => Mathf.Max(0f, collider.Radius),
                _ => 0f
            };

            return radius + collider.Center.magnitude;
        }

        private void TryQueueComboRicochet(
            RicochetRequest request,
            ref ProjectileComponent projectile)
        {
            if (!_sequenceStash.Has(request.Weapon) ||
                !_attackStash.Has(request.Weapon))
            {
                return;
            }

            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(request.Weapon);
            AttackComponent attack = _attackStash.Get(request.Weapon);

            if (!sequence.IsActive ||
                sequence.Id != request.SequenceId ||
                sequence.RicochetsCount >= attack.MaxRicochets ||
                sequence.RicochetPending ||
                !_targetSelector.TrySelectNearest(
                    request.Projectile,
                    request.HitTarget,
                    request.HitPosition,
                    projectile.TargetSearchRadius,
                    _candidates,
                    sequence.VisitedTargets,
                    false,
                    out Entity nextTarget,
                    out _))
            {
                return;
            }

            sequence.RicochetPending = true;
            sequence.PendingTarget = nextTarget;
        }

        public void Dispose()
        {
            _candidates.Clear();
        }
    }
}
