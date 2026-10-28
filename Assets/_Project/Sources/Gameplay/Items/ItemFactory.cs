using System.Collections.Generic;
using Configs;
using ECS.CommonComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;
using Zenject;

namespace Gameplay.Items
{
    public sealed class ItemFactory
    {
        private readonly World _world;

        private readonly Stash<ItemPickupComponent> _itemStash;

        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ItemViewComponent> _viewStash;

        private readonly Stash<AddExperienceEvent> _addExperienceEvent;

        private List<GemExpConfig> _gemConfigs;

        public ItemFactory(World world, List<GemExpConfig> gemConfigs)
        {
            _world = world;
            _gemConfigs = gemConfigs;

            _itemStash = world.GetStash<ItemPickupComponent>();

            _positionStash = world.GetStash<PositionComponent>();
            _viewStash = world.GetStash<ItemViewComponent>();
            _addExperienceEvent = world.GetStash<AddExperienceEvent>();
        }

        public GameObject Create(Vector3 position)
        {
            Entity entity = _world.CreateEntity();

            int randomIndex = UnityEngine.Random.Range(0, _gemConfigs.Count);

            GemExpConfig itemConfig = _gemConfigs[randomIndex];

            _itemStash.Set(entity, new ItemPickupComponent
            {
                Id = itemConfig.Id,
                Type = itemConfig.Type,
            });

            _positionStash.Set(entity, new PositionComponent { Position = position });

            GameObject itemObject = Object.Instantiate(itemConfig.Prefab, position, Quaternion.identity);

            _viewStash.Set(entity, new ItemViewComponent { View = itemObject });

            if (itemConfig.Type == ItemTypes.ExperienceGem)
                _addExperienceEvent.Set(entity, new AddExperienceEvent { Amount = itemConfig.AddExpAmount });

            return itemObject;
        }
    }
}