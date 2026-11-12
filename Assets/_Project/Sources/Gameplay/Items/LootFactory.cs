using System;
using System.Collections.Generic;
using Core.Game;
using ECS.CommonComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using Gameplay.View;
using LootConfigs;
using Scellecs.Morpeh;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Items
{
    public sealed class LootFactory
    {
        private readonly World _world;
        private readonly Dictionary<string, ObjectPool<EntityView>> _poolMap = new ();
        private readonly List<ObjectPool<EntityView>> _pools = new ();
        private readonly Transform _container;

        private readonly Stash<ItemPickupComponent> _itemStash;

        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ItemViewComponent> _viewStash;

        private readonly Stash<AddExperienceEvent> _addExperienceEvent;

        private LootDropDatabase _dropDatabase;
        private LootDatabase _lootDatabase;

        public LootFactory(
            World world,
            LootDropDatabase dropDatabase,
            LootDatabase lootDatabase,
            Transform container)
        {
            _world = world;

            _dropDatabase = dropDatabase;
            _lootDatabase = lootDatabase;

            _container = container;

            _itemStash = world.GetStash<ItemPickupComponent>();

            _positionStash = world.GetStash<PositionComponent>();
            _viewStash = world.GetStash<ItemViewComponent>();
            _addExperienceEvent = world.GetStash<AddExperienceEvent>();

            InitializeConfigs();
        }

        public EntityView Create(CreatureType enemyType, Vector3 position)
        {
            Entity entity = _world.CreateEntity();

            var lootList = _dropDatabase.GetLootForEnemy(enemyType);

            if (lootList == null)
            {
                Debug.Log($"Не указаны предметы дропа с врага {enemyType}");
                return null;
            }

            foreach (PossibleDropsEntry loot in lootList)
            {
                float randomRoll = UnityEngine.Random.Range(0f, 100f);

                if (randomRoll > loot.DropChance)
                {
                    LootDropItem item = loot.Item;

                    _itemStash.Set(entity, new ItemPickupComponent
                    {
                        Id = item.Id,
                        Type = item.Type,
                    });

                    _positionStash.Set(entity, new PositionComponent { Position = position });

                    EntityView itemObject = GetItem(item.Prefab, position);

                    _viewStash.Set(entity, new ItemViewComponent { View = itemObject });

                    if (item is LootExpDropItem expItem)
                        _addExperienceEvent.Set(entity, new AddExperienceEvent { Amount = expItem.AddExpAmount });

                    return itemObject;
                }
            }

            return null;
        }

        public void Release(EntityView view)
        {
            string cleanName = view.name.Replace("(Clone)", string.Empty).Trim();

            if (_poolMap.TryGetValue(cleanName, out var pool))
                pool.Release(view);
        }

        private void InitializeConfigs()
        {
            if (_lootDatabase == null || _lootDatabase.LootItems.Count == 0)
                throw new ArgumentNullException(nameof(_lootDatabase));

            foreach (LootDropItem config in _lootDatabase.LootItems)
            {
                _pools.Add(
                    new ObjectPool<EntityView>(
                        config.Prefab,
                        config.PoolCapacity,
                        _container));
            }

            foreach (var pool in _pools)
                _poolMap.Add(pool.Prefab.name, pool);
        }

        private EntityView GetItem(EntityView prefab, Vector3 position)
        {
            ObjectPool<EntityView> pool = _pools.Find(item => item.Prefab == prefab);

            EntityView itemObject = pool.GetElement();

            itemObject.transform.position = position;
            itemObject.transform.rotation = Quaternion.identity;
            itemObject.transform.localScale = pool.InitScale;

            itemObject.gameObject.SetActive(true);

            return itemObject;
        }
    }
}