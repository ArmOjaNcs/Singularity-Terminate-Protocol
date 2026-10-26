using Configs;
using Core.Services;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class LevelUpSystem : ISystem
    {
        private Filter _filter;
        private Stash<LevelUpEvent> _levelUpStash;
        private Stash<PlayerInventoryComponent> _inventoryStash;
        private Stash<StatsComponent> _stats;
        private UIService _uiService;
        private bool _isPausedByMe;

        public LevelUpSystem(UIService uiService)
        {
            _uiService = uiService;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _levelUpStash = World.GetStash<LevelUpEvent>();
            _inventoryStash = World.GetStash<PlayerInventoryComponent>();
            _stats = World.GetStash<StatsComponent>();

            _filter = World.Filter
                .With<LevelUpEvent>()
                .Build();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_uiService.IsLevelUpScreenOpen)
                return;

            if (_filter.IsEmpty())
            {
                if (_isPausedByMe)
                {
                    // use Pause System
                    Time.timeScale = 1f;
                    _isPausedByMe = false;
                }

                return;
            }

            foreach (var entity in _filter)
            {
                ref var levelUp = ref _levelUpStash.Get(entity);
                ref var inventory = ref _inventoryStash.Get(levelUp.Owner);
                ref var stats = ref _stats.Get(levelUp.Owner);

                // Pause
                Time.timeScale = 0f;
                _isPausedByMe = true;

                bool isLuckyStrike = UnityEngine.Random.value <= stats.Luck / 100f;

                _uiService.ShowLevelUpScreen(inventory, isLuckyStrike);
                World.RemoveEntity(entity);

                break;
            }
        }

        public void Dispose()
        {
        }
    }
}