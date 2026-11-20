using ECS.PlayerComponents;
using Scellecs.Morpeh;
using Zenject;

namespace UI
{
    public class LevelUpDebugButton : UIButton
    {
        private World _world;
        private Filter _playerFilter;
        private Stash<DebugLevelUpRequest> _requestStash;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
            _playerFilter = _world.Filter.With<PlayerTag>().Build();
            _requestStash = _world.GetStash<DebugLevelUpRequest>();
        }

        public override void HandleClick()
        {
            foreach (var playerEntity in _playerFilter)
            {
                _requestStash.Set(playerEntity, new DebugLevelUpRequest { Amount = 1 });
            }
        }
    }
}