using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class RicochetTargetSelector
    {
        private readonly SpatialGrid _grid;
        private readonly Filter _enemyFilter;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ProjectileHitComponent> _projectileHitStash;

        public RicochetTargetSelector(World world, SpatialGrid grid)
        {
            _grid = grid;
            _enemyFilter = world.Filter
                .With<EnemyTag>()
                .Without<DeathComponent>()
                .Build();
            _positionStash = world.GetStash<PositionComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _projectileHitStash = world.GetStash<ProjectileHitComponent>();
        }

        public bool TrySelectNearest(
            Entity projectile,
            Entity hitTarget,
            Vector3 origin,
            float radius,
            List<Entity> candidates,
            IList<Entity> excludedTargets,
            bool allowPreviouslyHitTargets,
            out Entity selectedTarget,
            out Vector3 direction)
        {
            selectedTarget = default;
            direction = Vector3.zero;

            if (radius <= 0f || !_projectileHitStash.Has(projectile))
                return false;

            _grid.Query(origin, radius, candidates);

            List<ProjectileHitRecord> records =
                _projectileHitStash.Get(projectile).Records;
            float nearestDistanceSquared = radius * radius;
            Entity nearestTarget = default;

            for (int i = 0; i < candidates.Count; i++)
            {
                Entity candidate = candidates[i];

                // Never bounce straight back to the target just hit when a
                // different living target is available. Repeat selection
                // applies only to other targets hit on previous legs.
                if (candidate == hitTarget ||
                    !_enemyFilter.Has(candidate) ||
                    !_hitBoxStash.Has(candidate) ||
                    !_positionStash.Has(candidate) ||
                    IsExcluded(excludedTargets, candidate) ||
                    (!allowPreviouslyHitTargets && WasAlreadyHit(records, candidate)))
                {
                    continue;
                }

                Vector3 offset = _positionStash.Get(candidate).Position - origin;
                offset.z = 0f;
                float distanceSquared = offset.sqrMagnitude;

                if (distanceSquared > nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearestTarget = candidate;
            }

            if (nearestTarget == default)
                return false;

            direction = _positionStash.Get(nearestTarget).Position - origin;
            direction.z = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.zero;
                return false;
            }

            direction.Normalize();
            selectedTarget = nearestTarget;
            return true;
        }

        private static bool IsExcluded(
            IList<Entity> excludedTargets,
            Entity target)
        {
            if (excludedTargets == null)
                return false;

            for (int i = 0; i < excludedTargets.Count; i++)
            {
                if (excludedTargets[i] == target)
                    return true;
            }

            return false;
        }

        private static bool WasAlreadyHit(
            List<ProjectileHitRecord> records,
            Entity target)
        {
            if (records == null)
                return false;

            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Target == target)
                    return true;
            }

            return false;
        }
    }
}
