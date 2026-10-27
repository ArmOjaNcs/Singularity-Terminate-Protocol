using Core.Game;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using ECS.WeaponSystems;
using EnemyConfigs;
using Gameplay.Creatures;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Enemy
{
    public sealed class EnemyFactory
    {
        private readonly World _world;
        private readonly CreatureFactory _creatureFactory;
        private readonly EnemyConfigsDatabase _database;
        private readonly Transform _container;

        private readonly Dictionary<CreatureType, ObjectPool<EnemyView>> _views =
            new Dictionary<CreatureType, ObjectPool<EnemyView>>();

        private readonly Stash<EnemyTag> _enemyTagStash;
        private readonly Stash<EnemyMovementVariationComponent>
            _movementVariationStash;
        private readonly Stash<EnemyNavigationStateComponent>
            _navigationStateStash;
        private readonly Stash<ViewComponent> _viewStash;
        private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private readonly Stash<EnemyViewComponent> _enemyViewStash;

        public EnemyFactory(
            World world,
            WeaponFactory weaponFactory,
            EnemyConfigsDatabase database,
            Transform container)
        {
            _world = world;
            _creatureFactory = new CreatureFactory(world, weaponFactory);
            _database = database;
            _container = container;

            _enemyTagStash = world.GetStash<EnemyTag>();
            _movementVariationStash =
                world.GetStash<EnemyMovementVariationComponent>();
            _navigationStateStash =
                world.GetStash<EnemyNavigationStateComponent>();
            _viewStash = world.GetStash<ViewComponent>();
            _animatedCreatureStash =
                world.GetStash<AnimatedCreatureComponent>();
            _enemyViewStash = world.GetStash<EnemyViewComponent>();

            InitializeConfigs();
        }

        public Entity Create(EnemyConfig config, Vector3 position)
        {
            if (config == null || config.HitBox == null)
            {
                return default;
            }

            if (!_views.TryGetValue(
                    config.CreatureType,
                    out ObjectPool<EnemyView> pool))
            {
                return default;
            }

            EnemyView view = pool.GetElement();

            if (view == null)
                return default;

            Entity entity = _world.CreateEntity();

            view.transform.position = position;
            view.ResetState();
            view.ResetAnimator();
            view.SetEntity(entity);

            _enemyTagStash.Set(entity, new EnemyTag());

            _creatureFactory.CreateCommonComponents(
                entity,
                config,
                position);

            _movementVariationStash.Set(
                entity,
                new EnemyMovementVariationComponent
                {
                    SpeedMultiplier = Random.Range(0.85f, 1.15f),
                    ApproachOffset =
                        Random.insideUnitCircle.normalized *
                        Random.Range(0.4f, 1.2f)
                });

            _navigationStateStash.Set(
                entity,
                new EnemyNavigationStateComponent
                {
                    StuckTime = 0f,
                    AvoidanceCheckTime = 0f,
                    AvoidanceSide = 0,
                    PreferredSide = Random.value < 0.5f ? -1 : 1
                });

            _world.GetStash<EnemyMovementPolicyComponent>().Set(
                entity,
                new EnemyMovementPolicyComponent
                {
                    MovePolicy = config.MovePolicy,
                    AttackPolicy = config.AttackPolicy,
                    PreferredAttackDistance = Mathf.Max(0f, config.PreferredAttackDistance)
                });

            _viewStash.Set(entity, new ViewComponent
            {
                View = view
            });

            _animatedCreatureStash.Set(
                entity,
                new AnimatedCreatureComponent
                {
                    View = view
                });

            _enemyViewStash.Set(entity, new EnemyViewComponent
            {
                View = view
            });

            _creatureFactory.CreateWeapons(entity, config);

            view.SetActive(true);

            return entity;
        }

        private void InitializeConfigs()
        {
            if (_database == null || _database.Enemies == null)
                return;

            foreach (EnemyConfig config in _database.Enemies)
            {
                if (config == null ||
                    config.Prefab == null ||
                    config.HitBox == null)
                {
                    continue;
                }

                if (_views.ContainsKey(config.CreatureType))
                {
                    Debug.LogWarning(
                        $"EnemyFactory: duplicate config for {config.CreatureType}.",
                        config);

                    continue;
                }

                EnemyView prefab = config.Prefab.GetComponent<EnemyView>();

                if (prefab == null)
                {
                    Debug.LogWarning(
                        $"EnemyFactory: prefab for {config.CreatureType} " +
                        "does not contain EnemyView.",
                        config);

                    continue;
                }

                _views.Add(
                    config.CreatureType,
                    new ObjectPool<EnemyView>(
                        prefab,
                        config.PoolCapacity,
                        _container));
            }
        }
    }
}
