using ECS.DamageComponents;
using Scellecs.Morpeh;

namespace ECS.DamageSystems
{
    public sealed class DamageStatisticsSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<DamageStatisticComponent> _statisticStash;

        private readonly DamageStatistics _statistics;

        public DamageStatisticsSystem(DamageStatistics statistics)
        {
            _statistics = statistics;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<DamageStatisticComponent>()
                .Build();

            _statisticStash =
                World.GetStash<DamageStatisticComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref DamageStatisticComponent statistic = ref _statisticStash.Get(entity);

                _statistics.Add(statistic);

                World.RemoveEntity(entity);
            }
        }

        public void Dispose() { }
    }
}