using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class RicochetSystem : ISystem
    {
        private RicochetTargetSelector _targetSelector;
        private readonly List<Entity> _candidates = new List<Entity>();

        private Filter _filter;
        private Stash<RicochetRequest> _requestStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<AttackSequenceComponent> _sequenceStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<ArcTrajectoryComponent> _arcTrajectoryStash;

        public RicochetSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        private readonly SpatialGrid _grid;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter.With<RicochetRequest>().Build();
            _requestStash = World.GetStash<RicochetRequest>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _sequenceStash = World.GetStash<AttackSequenceComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _arcTrajectoryStash = World.GetStash<ArcTrajectoryComponent>();
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
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                float chance = Mathf.Clamp01(projectile.RicochetChance / 100f);
                if (Random.value > chance)
                {
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

                World.RemoveEntity(requestEntity);
            }
        }

        private void TryApplyProjectileRicochet(
            RicochetRequest request,
            ref ProjectileComponent projectile)
        {
            if (projectile.RicochetsCount >= projectile.MaxRicochets ||
                !_targetSelector.TrySelectNearest(
                    request.Projectile,
                    request.HitTarget,
                    request.HitPosition,
                    projectile.TargetSearchRadius,
                    _candidates,
                    null,
                    out Entity target,
                    out Vector3 direction))
            {
                return;
            }

            projectile.Direction = direction;
            projectile.RicochetsCount++;

            if (_arcTrajectoryStash.Has(request.Projectile))
            {
                ref ArcTrajectoryComponent trajectory =
                    ref _arcTrajectoryStash.Get(request.Projectile);
                trajectory.Direction = direction;
                trajectory.CurrentTime = 0f;
            }
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
