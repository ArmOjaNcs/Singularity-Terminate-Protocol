using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileSortingSystem : ISystem
    {
        private const int SortingMultiplier = 100;
        private const int ProjectileSortingOffset = 10000;

        public World World { get; set; }

        private Filter _filter;

        private Stash<PositionComponent> _positionStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<ProjectileViewComponent>()
                .Build();

            _positionStash =
                World.GetStash<PositionComponent>();

            _projectileViewStash =
                World.GetStash<ProjectileViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                ref ProjectileViewComponent viewComponent =
                    ref _projectileViewStash.Get(entity);

                int sortingOrder =
                    ProjectileSortingOffset -
                    Mathf.RoundToInt(position.Position.y * SortingMultiplier);

                viewComponent.View.SetSortingOrder(sortingOrder);
            }
        }

        public void Dispose()
        {
        }
    }
}