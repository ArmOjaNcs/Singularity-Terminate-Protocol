using ECS.CommonComponents;
using ECS.ViewComponents;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class ViewPositionSystem : ISystem
    {
        private Filter _filter;
        private Stash<PositionComponent> _positionStash;
        private Stash<ViewComponent> _viewStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<ViewComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();

            _viewStash = World.GetStash<ViewComponent>();
            _positionStash = World.GetStash<PositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position = ref _positionStash.Get(entity);
                ref ViewComponent viewComponent = ref _viewStash.Get(entity);

                viewComponent.View.transform.position = position.Position;
            }
        }

        public void Dispose()
        {
        }
    }
}