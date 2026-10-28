using ECS.CommonComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using LootConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Items
{
    public sealed class ItemFactory
    {
        private readonly World _world;

        private readonly Stash<ItemPickupComponent> _itemStash;

        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ItemViewComponent> _viewStash;

        private readonly Stash<AddExperienceEvent> _addExperienceEvent;

        private LootDropDatabase _lootDatabase;

        public ItemFactory(World world, LootDropDatabase lootDatabase)
        {
            _world = world;
            _lootDatabase = lootDatabase;

            _itemStash = world.GetStash<ItemPickupComponent>();

            _positionStash = world.GetStash<PositionComponent>();
            _viewStash = world.GetStash<ItemViewComponent>();
            _addExperienceEvent = world.GetStash<AddExperienceEvent>();
        }

        public GameObject Create(CreatureType enemyType, Vector3 position)
        {
            Entity entity = _world.CreateEntity();

            var lootList = _lootDatabase.GetLootForEnemy(enemyType);

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

                    GameObject itemObject = Object.Instantiate(item.Prefab, position, Quaternion.identity);

                    _viewStash.Set(entity, new ItemViewComponent { View = itemObject });

                    if (item is LootExpDropItem expItem)
                        _addExperienceEvent.Set(entity, new AddExperienceEvent { Amount = expItem.AddExpAmount });

                    return itemObject;
                }
            }

            return null;
        }
    }
}