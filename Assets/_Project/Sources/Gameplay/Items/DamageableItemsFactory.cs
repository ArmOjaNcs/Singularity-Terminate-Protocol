using System;
using System.Collections.Generic;
using Configs;
using Core.Game;
using DamageableConfigs;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.ViewComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Items
{
    public sealed class DamageableItemsFactory
    {
        private readonly World _world;
        private readonly Dictionary<CreatureType, ObjectPool<AnimatedCreature>> _pools = new ();
        private readonly Stash<DamageableTag> _damageableTagStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ViewComponent> _viewStash;
        private readonly Stash<StatsComponent> _statsStash;
        private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private readonly DamageableItemsDatabase _database;
        private Transform _container;

        public DamageableItemsFactory(
            World world,
            DamageableItemsDatabase damageableItemsDatabase,
            Transform container)
        {
            _world = world;
            _container = container;
            _database = damageableItemsDatabase;

            _damageableTagStash = world.GetStash<DamageableTag>();
            _positionStash = world.GetStash<PositionComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _viewStash = world.GetStash<ViewComponent>();
            _statsStash = world.GetStash<StatsComponent>();
            _animatedCreatureStash = world.GetStash<AnimatedCreatureComponent>();

            InitializeConfigs();
        }

        public Entity Create(DamageableItemConfig config, Vector3 position)
        {
            if (config == null || config.HitBox == null)
                return default;

            if (_pools.TryGetValue(
                config.Type,
                out ObjectPool<AnimatedCreature> pool) == false)
            {
                return default;
            }

            AnimatedCreature view = pool.GetElement();

            if (view == null)
                return default;

            Entity entity = _world.CreateEntity();

            view.transform.position = position;
            view.ResetState();
            view.ResetAnimator();
            view.SetEntity(entity);

            SetComponents(config, view, entity, position);

            view.SetActive(true);

            return entity;
        }

        private void InitializeConfigs()
        {
            if (_database == null || _database.Items == null)
                throw new ArgumentNullException(nameof(_database));

            foreach (var config in _database.Items)
            {
                if (config == null ||
                    config.Prefab == null ||
                    config.HitBox == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                AnimatedCreature prefab = config.Prefab.GetComponent<AnimatedCreature>();

                _pools.Add(config.Type, new ObjectPool<AnimatedCreature>(prefab, config.PoolCapacity, _container));
            }
        }

        private void SetComponents(DamageableItemConfig config, AnimatedCreature view, Entity entity, Vector3 position)
        {
            _damageableTagStash.Set(entity, new DamageableTag());

            _positionStash.Set(entity, new PositionComponent
            {
                Position = position,
            });

            _animatedCreatureStash.Set(entity, new AnimatedCreatureComponent
            {
                View = view,
            });

            _statsStash.Set(entity, new StatsComponent
            {
                CreatureType = config.Type,
                MaxHealth = config.Health,
            });

            _viewStash.Set(entity, new ViewComponent
            {
                View = view,
            });

            _healthStash.Set(entity, new HealthComponent
            {
                Current = config.Health,
            });

            ColliderConfig hitboxConfig = config.HitBox;
            ColliderComponent colliderComponent = new ColliderComponent
            {
                Type = hitboxConfig.Type,
                Radius = hitboxConfig.Radius,
                Length = hitboxConfig.Length,
                Size = hitboxConfig.Size,
                Center = hitboxConfig.Center,
                Angle = 0f,
                CapsuleOrientation = hitboxConfig.CapsuleOrientation,
            };

            _hitBoxStash.Set(entity, new HitBoxComponent
            {
                Collider = colliderComponent,
            });
        }
    }
}