using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Gameplay.View;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileCollisionCheckSystem : ISystem
    {
        public World World { get; set; }

        private readonly SpatialGrid _grid;
        private readonly List<Entity> _candidates =
            new List<Entity>();

        private Filter _filter;
        private Filter _enemyFilter;
        private Filter _playerFilter;

        private Stash<ProjectileComponent> _projectileStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;
        private Stash<DamageRequest> _damageRequestStash;

        public ProjectileCollisionCheckSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .Build();

            _enemyFilter = World.Filter
                .With<EnemyTag>()
                .Build();

            _playerFilter = World.Filter
                .With<PlayerTag>()
                .Build();

            _projectileStash =
                World.GetStash<ProjectileComponent>();

            _positionStash =
                World.GetStash<PositionComponent>();

            _projectileViewStash =
                World.GetStash<ProjectileViewComponent>();

            _damageRequestStash =
                World.GetStash<DamageRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity projectile in _filter)
            {
                ref ProjectileComponent projectileComponent =
                    ref _projectileStash.Get(projectile);

                ref PositionComponent position =
                    ref _positionStash.Get(projectile);

                position.Position +=
                    projectileComponent.Direction *
                    projectileComponent.Speed *
                    deltaTime;

                if (!TryFindTarget(
                        position.Position,
                        projectileComponent,
                        out Entity target))
                    continue;

                CreateDamageRequest(
                    projectileComponent.Owner,
                    target,
                    projectileComponent.Damage);

                DisableView(projectile);

                World.RemoveEntity(projectile);
            }
        }

        private bool TryFindTarget(
            Vector3 position,
            ProjectileComponent projectile,
            out Entity target)
        {
            target = default;

            _grid.Query(
                position,
                projectile.Radius,
                _candidates);

            for (int i = 0; i < _candidates.Count; i++)
            {
                Entity candidate = _candidates[i];

                if (candidate == projectile.Owner)
                    continue;

                if (!IsValidTarget(
                        candidate,
                        projectile.TargetType))
                    continue;

                target = candidate;

                return true;
            }

            return false;
        }

        private bool IsValidTarget(
            Entity entity,
            AttackTargetType targetType)
        {
            return targetType switch
            {
                AttackTargetType.Enemy => _enemyFilter.Has(entity),
                AttackTargetType.Player => _playerFilter.Has(entity),
                _ => false
            };
        }

        private void CreateDamageRequest(
            Entity source,
            Entity target,
            float damage)
        {
            Entity requestEntity = World.CreateEntity();

            _damageRequestStash.Set(
                requestEntity,
                new DamageRequest
                {
                    Source = source,
                    Target = target,
                    Damage = damage
                });
        }

        private void DisableView(Entity projectile)
        {
            if (!_projectileViewStash.Has(projectile))
                return;

            AnimatedView view =
                _projectileViewStash.Get(projectile).View;

            if (view != null)
                view.SetActive(false);
        }

        public void Dispose()
        {
        }
    }
}