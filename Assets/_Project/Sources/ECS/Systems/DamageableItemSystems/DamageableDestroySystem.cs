using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using Gameplay;
using Gameplay.View;
using Scellecs.Morpeh;

public sealed class DamageableDestroySystem : ISystem
{
    private Filter _filter;

    private Stash<DeathComponent> _deathStash;
    private Stash<AnimatedCreatureComponent> _animatedCreatureStash;
    private Stash<ViewComponent> _viewStash;
    private Stash<AddLootDropEvent> _lootSpawnStash;
    private Stash<PositionComponent> _positionStash;
    private Stash<StatsComponent> _statsStash;

    public World World { get; set; }

    public void OnAwake()
    {
        _filter = World.Filter
            .With<DamageableTag>()
            .With<DeathComponent>()
            .With<AnimatedCreatureComponent>()
            .Build();

        _deathStash = World.GetStash<DeathComponent>();
        _animatedCreatureStash = World.GetStash<AnimatedCreatureComponent>();
        _viewStash = World.GetStash<ViewComponent>();
        _lootSpawnStash = World.GetStash<AddLootDropEvent>();
        _positionStash = World.GetStash<PositionComponent>();
        _statsStash = World.GetStash<StatsComponent>();
    }

    public void OnUpdate(float deltaTime)
    {
        foreach (Entity entity in _filter)
        {
            ref DeathComponent death = ref _deathStash.Get(entity);

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

        SpawnLoot(entity);
        CommonFunctions.CreateCleanupRequest(World, entity);
    }

    private void SpawnLoot(Entity entity)
    {
        if (_positionStash.Has(entity))
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