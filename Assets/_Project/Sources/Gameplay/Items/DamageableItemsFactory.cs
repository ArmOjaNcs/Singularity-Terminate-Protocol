using System;
using System.Collections.Generic;
using Configs;
using Core.Game;
using DamageableConfigs;
using ECS.CommonComponents;
using ECS.ViewComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Items
{
    public sealed class DamageableItemsFactory
    {
        private readonly World _world;
        private readonly Dictionary<DamageableItemType, ObjectPool<AnimatedCreature>> _pools = new ();
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<ViewComponent> _viewStash;
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

            _healthStash = world.GetStash<HealthComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _viewStash = world.GetStash<ViewComponent>();

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

            SetComponents(config, view, entity);

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

        private void SetComponents(DamageableItemConfig config, AnimatedCreature view, Entity entity)
        {
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