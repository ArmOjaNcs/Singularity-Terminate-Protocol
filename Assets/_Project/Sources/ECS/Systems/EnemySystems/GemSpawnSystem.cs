using ECS.EnemyComponents;
using Gameplay.Items;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class GemSpawnSystem : ISystem
    {
        private readonly ItemFactory _itemFactory;
        private Filter _filter;
        private Stash<AddExperienceGemEvent> _addGemStash;

        public GemSpawnSystem(ItemFactory itemFactory)
        {
            _itemFactory = itemFactory;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<AddExperienceGemEvent>()
                .Build();

            _addGemStash = World.GetStash<AddExperienceGemEvent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_itemFactory == null)
                return;

            foreach (Entity entity in _filter)
            {
                ref var addGemEvent = ref _addGemStash.Get(entity);

                _itemFactory.Create(addGemEvent.Position);

                World.RemoveEntity(entity);
            }
        }

        public void Dispose()
        {
        }
    }
}