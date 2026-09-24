using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class ViewPositionSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<PositionComponent> _positionStash;
        private Stash<ViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<ViewComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();
            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var position = ref _positionStash.Get(entity);
                ref var viewComponent = ref _viewStash.Get(entity);

                viewComponent.View.transform.position = position.Position;
            }
        }

        public void Dispose()
        {
        }
    }
}