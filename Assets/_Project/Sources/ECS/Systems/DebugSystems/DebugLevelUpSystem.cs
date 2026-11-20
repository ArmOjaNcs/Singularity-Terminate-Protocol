using ECS.PlayerComponents;
using ECS.PlayerSystems;
using Scellecs.Morpeh;

namespace ECS.DebugSystems
{
    public sealed class DebugLevelUpSystem : ISystem
    {
        private Filter _filter;
        private Stash<DebugLevelUpRequest> _requestStash;
        private Stash<PlayerExperienceComponent> _levelStash;
        private Stash<LevelUpEvent> _levelUpStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<PlayerExperienceComponent>()
                .With<DebugLevelUpRequest>()
                .Build();

            _requestStash = World.GetStash<DebugLevelUpRequest>();
            _levelStash = World.GetStash<PlayerExperienceComponent>();
            _levelUpStash = World.GetStash<LevelUpEvent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var request = ref _requestStash.Get(entity);
                ref var level = ref _levelStash.Get(entity);

                level.CurrentXP.Value = level.NextLevelXP.Value;
                level.CurrentLevel.Value++;

                Entity levelUpEntity = World.CreateEntity();

                _levelUpStash.Set(levelUpEntity, new LevelUpEvent
                {
                    NewLevel = level.CurrentLevel.Value,
                    Owner = entity,
                });

                _requestStash.Remove(entity);
            }
        }

        public void Dispose()
        {
        }
    }
}