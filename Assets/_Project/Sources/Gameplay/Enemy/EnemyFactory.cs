using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using EnemyConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Enemy
{
    public sealed class EnemyFactory
    {
        private readonly World _world;

        private readonly Stash<EnemyTag> _enemyTagStash;
        private readonly Stash<StatsComponent> _statsStash;
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<MovementComponent> _movementStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<NavigationRadiusComponent> _navigationRadiusStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<TargetComponent> _targetStash;

        private readonly Stash<ViewComponent> _viewStash;
        private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private readonly Stash<EnemyViewComponent> _enemyViewStash;

        public EnemyFactory(World world)
        {
            _world = world;

            _enemyTagStash = world.GetStash<EnemyTag>();
            _statsStash = world.GetStash<StatsComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _navigationRadiusStash = world.GetStash<NavigationRadiusComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _targetStash = world.GetStash<TargetComponent>();

            _viewStash = world.GetStash<ViewComponent>();
            _animatedCreatureStash = world.GetStash<AnimatedCreatureComponent>();
            _enemyViewStash = world.GetStash<EnemyViewComponent>();
        }

        public Entity Create(EnemyConfig config, Vector3 position)
        {
            Entity entity = _world.CreateEntity();

            StatsComponent stats = new StatsComponent
            {
                MaxHealth = config.Stats.MaxHealth,
                HealthRegeneration = config.Stats.HealthRegeneration,
                Damage = config.Stats.Damage,
                Luck = config.Stats.Luck,
                Defence = config.Stats.Defence,
                Speed = config.Stats.Speed
            };

            _enemyTagStash.Set(entity, new EnemyTag());
            _statsStash.Set(entity, stats);
            _healthStash.Set(entity, new HealthComponent { Current = stats.MaxHealth });
            _movementStash.Set(entity, new MovementComponent { Direction = Vector3.zero });
            _positionStash.Set(entity, new PositionComponent { Position = position });

            _navigationRadiusStash.Set(
                entity,
                new NavigationRadiusComponent
                {
                    Radius = config.Stats.NavigationRadius
                });

            _attackStash.Set(
                entity,
                new AttackComponent
                {
                    Damage = config.Stats.Damage,
                    AttackRadius = 25f,
                    AttackCooldown = 1f,
                    CurrentCooldown = 0f,
                    TargetType = AttackTargetType.Player,
                    SourceId = string.IsNullOrEmpty(config.Id)
                        ? config.name
                        : config.Id,
                    ProjectileConfig = config.ProjectileConfig
                });

            _targetStash.Set(entity, new TargetComponent());

            GameObject enemyObject = Object.Instantiate(config.Prefab);
            enemyObject.transform.position = position;

            EnemyView view = enemyObject.GetComponent<EnemyView>();

            if (view == null)
            {
                Debug.LogError($"Enemy prefab '{config.name}' does not contain EnemyView.");
                Object.Destroy(enemyObject);
                _world.RemoveEntity(entity);

                return default;
            }

            view.SetEntity(entity);

            _viewStash.Set(
                entity,
                new ViewComponent
                {
                    View = view
                });

            _animatedCreatureStash.Set(
                entity,
                new AnimatedCreatureComponent
                {
                    View = view
                });

            _enemyViewStash.Set(
                entity,
                new EnemyViewComponent
                {
                    View = view
                });

            return entity;
        }
    }
}