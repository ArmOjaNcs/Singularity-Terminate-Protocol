using ECS.ViewComponents;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class ViewPauseSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<ViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewComponent>()
                .Build();

            _viewStash = World.GetStash<ViewComponent>();
        }

        public void Pause()
        {
            foreach (Entity entity in _filter)
            {
                _viewStash.Get(entity).View.Pause();
            }
        }

        public void Play()
        {
            foreach (Entity entity in _filter)
            {
                _viewStash.Get(entity).View.Play();
            }
        }

        public void OnUpdate(float deltaTime)
        {
        }

        public void Dispose()
        {
        }
    }
}