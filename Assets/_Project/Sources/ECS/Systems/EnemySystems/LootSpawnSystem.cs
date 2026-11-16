using ECS.EnemyComponents;
using Gameplay.Items;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class LootSpawnSystem : ISystem
    {
        private readonly LootFactory _lootFactory;
        private Filter _filter;
        private Stash<AddLootDropEvent> _addLootStash;

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

            _addLootStash = World.GetStash<AddLootDropEvent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_lootFactory == null)
                return;

            foreach (Entity entity in _filter)
            {
                ref var addLootEvent = ref _addLootStash.Get(entity);

                _lootFactory.Create(addLootEvent.CreatureType, addLootEvent.Position);

                World.RemoveEntity(entity);
            }
        }

        public void Dispose()
        {
        }
    }
}