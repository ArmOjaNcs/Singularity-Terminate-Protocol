using ECS.EnemyComponents;
using Gameplay.Items;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class LootSpawnSystem : ISystem
    {
        private readonly ItemFactory _itemFactory;
        private Filter _filter;
        private Stash<AddLootDropEvent> _addGemStash;

        public LootSpawnSystem(ItemFactory itemFactory)
        {
            _itemFactory = itemFactory;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<AddLootDropEvent>()
                .Build();

            _addGemStash = World.GetStash<AddLootDropEvent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_itemFactory == null)
                return;

            foreach (Entity entity in _filter)
            {
                ref var addGemEvent = ref _addGemStash.Get(entity);

                _itemFactory.Create(addGemEvent.EnemyType, addGemEvent.Position);

                World.RemoveEntity(entity);
            }
        }

        public void Dispose()
        {
        }
    }
}