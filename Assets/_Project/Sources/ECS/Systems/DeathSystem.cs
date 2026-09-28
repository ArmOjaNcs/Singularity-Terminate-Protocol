using ECS.CommonComponents;
using Gameplay.Common;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class DeathSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<DeathRequest> _deathRequestStash;
        private Stash<ViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<DeathRequest>()
                .Build();

            _deathRequestStash = World.GetStash<DeathRequest>();
            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _filter)
            {
                ref DeathRequest request = ref _deathRequestStash.Get(requestEntity);

                if (request.Target == default)
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (_viewStash.Has(request.Target))
                {
                    EntityView view = _viewStash.Get(request.Target).View;

                    if (view != null)
                        view.SetActive(false);
                }

                World.RemoveEntity(request.Target);
                World.RemoveEntity(requestEntity);
            }
        }

        public void Dispose()
        {
        }
    }
}