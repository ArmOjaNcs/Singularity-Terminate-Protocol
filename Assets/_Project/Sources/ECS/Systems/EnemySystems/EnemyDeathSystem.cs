using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using ECS.WeaponComponents;
using Gameplay;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class EnemyDeathSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private Stash<ViewComponent> _viewStash;
        private Stash<AddLootDropEvent> _lootSpawnStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<BattleStatisticComponent> _battleStatsStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EnemyTag>()
                .With<DeathComponent>()
                .With<AnimatedCreatureComponent>()
                .Build();

            _deathStash = World.GetStash<DeathComponent>();
            _animatedCreatureStash = World.GetStash<AnimatedCreatureComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _viewStash = World.GetStash<ViewComponent>();
            _lootSpawnStash = World.GetStash<AddLootDropEvent>();
            _positionStash = World.GetStash<PositionComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _battleStatsStash = World.GetStash<BattleStatisticComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref DeathComponent death = ref _deathStash.Get(entity);
                ref StatsComponent stats = ref _statsStash.Get(entity);

                if (death.AnimationStarted)
                    continue;

                AnimatedCreature view = _animatedCreatureStash.Get(entity).View;

                if (view == null)
                    continue;

                death.AnimationStarted = true;

                view.OnDeathPerformed -= OnDeathPerformed;
                view.OnDeathPerformed += OnDeathPerformed;

                view.SetDeath();
            }
        }

        public void Dispose()
        {
        }

        private void OnDeathPerformed(AnimatedCreature view)
        {
            view.OnDeathPerformed -= OnDeathPerformed;

            Entity entity = view.Entity;

            if (entity == default)
                return;

            if (_viewStash.Has(entity))
            {
                ViewComponent viewComponent = _viewStash.Get(entity);

                if (viewComponent.View != null)
                    viewComponent.View.SetActive(false);
            }

            AddKilledStatistics(entity);
            SpawnLoot(entity);
            CommonFunctions.CreateCleanupRequest(World, entity);
        }

        private void AddKilledStatistics(Entity entity)
        {
            Entity battleStats = World.CreateEntity();

            _battleStatsStash.Set(battleStats, new BattleStatisticComponent
            {
                IsKilledEnemy = true,
            });
        }

        private void SpawnLoot(Entity entity)
        {
            if (_statsStash.Has(entity) && _positionStash.Has(entity))
            {
                Entity lootSpawner = World.CreateEntity();
                PositionComponent position = _positionStash.Get(entity);
                StatsComponent stats = _statsStash.Get(entity);

                _lootSpawnStash.Set(lootSpawner, new AddLootDropEvent
                {
                    Position = position.Position,
                    CreatureType = stats.CreatureType,
                });
            }
        }
    }
}