using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class LevelUpSystem : ISystem
    {
        private Filter _filter;
        private Stash<LevelUpEvent> _levelUpStash;

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
            foreach (var entity in _filter)
            {
                ref var levelUp = ref _levelUpStash.Get(entity);

                Debug.Log($"[Level Up] Персонаж достиг {levelUp.NewLevel} уровня!");
                _levelUpStash.Remove(entity);
            }
        }

        public void Dispose()
        {
        }
    }
}