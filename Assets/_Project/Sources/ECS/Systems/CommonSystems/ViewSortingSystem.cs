using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class ViewSortingSystem : ISystem
    {
        private const int SortingMultiplier = 100;

        private Filter _filter;
        private Stash<PositionComponent> _positionStash;
        private Stash<ViewComponent> _viewStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<ViewComponent>()
                .Without<ProjectileViewComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();

            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position = ref _positionStash.Get(entity);

                ref ViewComponent viewComponent = ref _viewStash.Get(entity);

                int sortingOrder = -Mathf.RoundToInt(position.Position.y * SortingMultiplier);

                viewComponent.View.SetSortingOrder(sortingOrder);
            }
        }

        public void Dispose()
        {
        }
    }
}