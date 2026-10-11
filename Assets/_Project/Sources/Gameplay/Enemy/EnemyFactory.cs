using Configs;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using ECS.WeaponSystems;
using EnemyConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Enemy
{
    public sealed class EnemyFactory
    {
        private readonly World _world;
        private readonly WeaponFactory _weaponFactory;

        private readonly Stash<EnemyTag> _enemyTagStash;
        private readonly Stash<StatsComponent> _statsStash;
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<MovementComponent> _movementStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ColliderComponent> _colliderStash;

        private readonly Stash<ViewComponent> _viewStash;
        private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private readonly Stash<EnemyViewComponent> _enemyViewStash;

        public EnemyFactory(World world, WeaponFactory weaponFactory)
        {
            _world = world;
            _weaponFactory = weaponFactory;

            _enemyTagStash = world.GetStash<EnemyTag>();
            _statsStash = world.GetStash<StatsComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();

            _viewStash = world.GetStash<ViewComponent>();
            _animatedCreatureStash = world.GetStash<AnimatedCreatureComponent>();
            _enemyViewStash = world.GetStash<EnemyViewComponent>();
        }

        public GameObject Create(EnemyConfig config, Vector3 position)
        {
            Entity entity = _world.CreateEntity();

            CreateStats(entity, config);
            CreateGameplayComponents(entity, config, position);

            GameObject enemyObject = CreateView(entity, config, position);

            if (enemyObject == null)
                return null;

            CreateWeapon(entity, config);

            return enemyObject;
        }

        private void CreateStats(Entity entity, EnemyConfig config)
        {
            BaseStats baseStats = config.Stats;

            StatsComponent stats = new StatsComponent
            {
                CreatureType = config.CreatureType,
                MaxHealth = baseStats.MaxHealth,
                HealthRegeneration = baseStats.HealthRegeneration,
                Damage = baseStats.Damage,
                Luck = baseStats.Luck,
                Defence = baseStats.Defence,
                Speed = baseStats.Speed
            };

            _statsStash.Set(entity, stats);

            _healthStash.Set(entity, new HealthComponent
            {
                Current = stats.MaxHealth
            });
        }

        private void CreateGameplayComponents(Entity entity, EnemyConfig config, Vector3 position)
        {
            _enemyTagStash.Set(entity, new EnemyTag());

            _movementStash.Set(entity, new MovementComponent
            {
                Direction = Vector3.zero
            });

            _positionStash.Set(entity, new PositionComponent
            {
                Position = position
            });

            _colliderStash.Set(
                entity,
                new ColliderComponent
                {
                    Type = config.Collider.Type,
                    Radius = config.Collider.Radius,
                    Length = config.Collider.Length,
                    Size = config.Collider.Size,
                    Angle = 0f,
                    CapsuleOrientation = config.Collider.CapsuleOrientation
                });
        }

        private GameObject CreateView(Entity entity, EnemyConfig config, Vector3 position)
        {
            GameObject enemyObject = Object.Instantiate(config.Prefab);
            enemyObject.transform.position = position;

            EnemyView view = enemyObject.GetComponent<EnemyView>();

            if (view == null)
            {
                Object.Destroy(enemyObject);
                _world.RemoveEntity(entity);

                return null;
            }

            view.SetEntity(entity);

            _viewStash.Set(entity, new ViewComponent
            {
                View = view
            });

            _animatedCreatureStash.Set(entity, new AnimatedCreatureComponent
            {
                View = view
            });

            _enemyViewStash.Set(entity, new EnemyViewComponent
            {
                View = view
            });

            return enemyObject;
        }

        private void CreateWeapon(Entity entity, EnemyConfig config)
        {
            if (config.WeaponConfig == null)
            {
                return;
            }

            _weaponFactory.Create(config.WeaponConfig, entity);
        }
    }
}