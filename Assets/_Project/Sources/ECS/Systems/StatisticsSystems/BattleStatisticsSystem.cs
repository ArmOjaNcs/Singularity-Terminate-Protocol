using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.DamageSystems
{
    public sealed class BattleStatisticsSystem : ISystem
    {
        public World World { get; set; }

        private readonly BattleStatistics _statistics;

        private Filter _filter;

        private Stash<BattleStatisticComponent> _statisticStash;

        public BattleStatisticsSystem(BattleStatistics statistics)
        {
            _statistics = statistics;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BattleStatisticComponent>()
                .Build();

            _statisticStash =
                World.GetStash<BattleStatisticComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref BattleStatisticComponent statistic = ref _statisticStash.Get(entity);

                if (statistic.IsKilledEnemy)
                    _statistics.AddKilledEnemy();

                if (statistic.GainedCredits > 0)
                    _statistics.AddGainedCredits(statistic.GainedCredits);

                if (statistic.Damage > 0)
                    _statistics.Add(statistic);

                World.RemoveEntity(entity);
            }
        }

        public void Dispose() { }
    }
}