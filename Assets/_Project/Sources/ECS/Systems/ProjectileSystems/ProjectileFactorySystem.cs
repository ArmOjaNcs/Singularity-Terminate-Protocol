using Core.Game;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.ViewComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileFactorySystem : ISystem
    {
        public World World { get; set; }

        private readonly Transform _container;

        private readonly Dictionary<string, ObjectPool<AnimatedView>> _projectilePools =
            new Dictionary<string, ObjectPool<AnimatedView>>();

        private Filter _filter;

        private Stash<AttackRequest> _attackRequestStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<ProjectileComponent> _projectileStash;

        private Stash<ViewComponent> _viewStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;

        public ProjectileFactorySystem(Transform container)
        {
            _container = container;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<AttackRequest>()
                .With<AttackComponent>()
                .With<TargetComponent>()
                .With<PositionComponent>()
                .Build();

            _attackRequestStash = World.GetStash<AttackRequest>();
            _attackStash = World.GetStash<AttackComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();

            _viewStash = World.GetStash<ViewComponent>();
            _projectileViewStash =
                World.GetStash<ProjectileViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref AttackRequest request =
                    ref _attackRequestStash.Get(entity);

                if (!_attackStash.Has(request.Source))
                {
                    _attackRequestStash.Remove(entity);
                    continue;
                }

                if (!_targetStash.Has(request.Source))
                {
                    _attackRequestStash.Remove(entity);
                    continue;
                }

                ref TargetComponent target =
                    ref _targetStash.Get(request.Source);

                if (target.Target == default ||
                    !_positionStash.Has(target.Target))
                {
                    _attackRequestStash.Remove(entity);
                    continue;
                }

                ref AttackComponent attack =
                    ref _attackStash.Get(request.Source);

                ref PositionComponent position =
                    ref _positionStash.Get(request.Source);

                ref PositionComponent targetPosition =
                    ref _positionStash.Get(target.Target);

                Vector3 direction =
                    targetPosition.Position - position.Position;

                if (direction != Vector3.zero)
                    direction.Normalize();

                Entity projectile = World.CreateEntity();

                _positionStash.Set(
                    projectile,
                    new PositionComponent
                    {
                        Position = position.Position
                    });

                float speed = attack.ProjectileConfig != null
                    ? attack.ProjectileConfig.Speed
                    : 0f;

                float lifetime = attack.ProjectileConfig != null
                    ? attack.ProjectileConfig.Lifetime
                    : 0f;

                float radius = attack.ProjectileConfig != null
                    ? attack.ProjectileConfig.Radius
                    : attack.AttackRadius;

                _projectileStash.Set(
                    projectile,
                    new ProjectileComponent
                    {
                        Owner = request.Source,
                        Damage = attack.Damage,
                        Speed = speed,
                        Lifetime = lifetime,
                        CurrentLifetime = 0f,
                        Radius = radius,
                        Direction = direction,
                        TargetType = attack.TargetType
                    });

                if (attack.ProjectileConfig != null &&
                    attack.ProjectileConfig.Prefab != null)
                {
                    CreateView(
                        projectile,
                        position.Position,
                        attack.SourceId,
                        attack.ProjectileConfig,
                        direction);
                }

                _attackRequestStash.Remove(entity);
            }
        }

        private void CreateView(
            Entity projectile,
            Vector3 position,
            string sourceId,
            EnemyConfigs.ProjectileConfig config,
            Vector3 direction)
        {
            if (!_projectilePools.TryGetValue(
                    sourceId,
                    out ObjectPool<AnimatedView> pool))
            {
                pool = new ObjectPool<AnimatedView>(
                    config.Prefab,
                    config.PoolCapacity,
                    _container);

                _projectilePools.Add(sourceId, pool);
            }

            AnimatedView view = pool.GetElement();

            view.SetEntity(projectile);
            view.transform.position = position;
            view.transform.right = direction;
            view.SetActive(true);

            _viewStash.Set(
                projectile,
                new ViewComponent
                {
                    View = view
                });

            _projectileViewStash.Set(
                projectile,
                new ProjectileViewComponent
                {
                    View = view
                });
        }

        public void Dispose()
        {
        }
    }
}