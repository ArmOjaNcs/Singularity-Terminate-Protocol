using ECS.CommonComponents;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class ViewPositionSystem : ISystem
    {
        private Filter _filter;
        private Stash<PositionComponent> _positionStash;
        private Stash<ViewComponent> _viewStash;
        private Stash<TwistStatusComponent> _twistStatusStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<ViewComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();

            _viewStash = World.GetStash<ViewComponent>();
            _twistStatusStash = World.GetStash<TwistStatusComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position = ref _positionStash.Get(entity);
                ref ViewComponent viewComponent = ref _viewStash.Get(entity);

                Vector3 visualOffset = _twistStatusStash.Has(entity)
                    ? _twistStatusStash.Get(entity).VisualOffset
                    : Vector3.zero;
                viewComponent.View.transform.position =
                    position.Position + visualOffset;
            }
        }

        public void Dispose()
        {
        }
    }
}
