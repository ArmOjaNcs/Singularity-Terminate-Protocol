using ECS.EnemyComponents;
using Gameplay.Items;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class LootSpawnSystem : ISystem
    {
        private readonly LootFactory _lootFactory;
        private Filter _filter;
        private Stash<AddLootDropEvent> _addGemStash;

        public LootSpawnSystem(LootFactory lootFactory)
        {
            _lootFactory = lootFactory;
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
            if (_lootFactory == null)
                return;

            foreach (Entity entity in _filter)
            {
                ref var addGemEvent = ref _addGemStash.Get(entity);

                _lootFactory.Create(addGemEvent.EnemyType, addGemEvent.Position);

                World.RemoveEntity(entity);
            }
        }

        public void Dispose()
        {
        }
    }
}