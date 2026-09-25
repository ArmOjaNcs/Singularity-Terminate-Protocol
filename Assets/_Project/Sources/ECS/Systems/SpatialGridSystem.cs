using ECS.CommonComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class SpatialGridSystem : ISystem
    {
        public World World { get; set; }

        private readonly SpatialGrid _grid;

        private Filter _filter;
        private Stash<PositionComponent> _positionStash;

        public SpatialGridSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .Build();

            _positionStash =
                World.GetStash<PositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            _grid.Clear();

            foreach (Entity entity in _filter)
            {
                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                _grid.Add(
                    entity,
                    position.Position);
            }
        }

        public void Dispose()
        {
        }
    }
}