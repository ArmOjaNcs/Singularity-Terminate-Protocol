using Core.Services;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class LevelUpSystem : ISystem
    {
        private Filter _filter;
        private Stash<LevelUpEvent> _levelUpStash;
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

                // Pause
                Time.timeScale = 0f;
                _isPausedByMe = true;

                _uiService.ShowLevelUpScreen();
                World.RemoveEntity(entity);

                break;
            }
        }

        public void Dispose()
        {
        }
    }
}