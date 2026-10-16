using ECS.PlayerComponents;
using Scellecs.Morpeh;
using UpgradeConfigs;

public sealed class PlayerUpgradeSystem : ISystem
{
    private Filter _eventFilter;
    private Filter _playerFilter;

    private Stash<UpgradeSelectedEvent> _eventStash;
    private Stash<PlayerInventoryComponent> _inventoryStash;

    public World World { get; set; }

    public void OnAwake()
    {
        _eventFilter = World.Filter.With<UpgradeSelectedEvent>().Build();
        _playerFilter = World.Filter.With<PlayerInventoryComponent>().Build();

        _eventStash = World.GetStash<UpgradeSelectedEvent>();
        _inventoryStash = World.GetStash<PlayerInventoryComponent>();
    }

    public void OnUpdate(float deltaTime)
    {
        if (_eventFilter.IsEmpty())
            return;

        foreach (var eventEntity in _eventFilter)
        {
            ref var eventData = ref _eventStash.Get(eventEntity);
            UpgradeConfig upgrade = eventData.SelectedUpgrade;

            foreach (var playerEntity in _playerFilter)
            {
                ref var inventory = ref _inventoryStash.Get(playerEntity);

                upgrade.Apply(playerEntity, inventory);
            }

            World.RemoveEntity(eventEntity);
        }
    }

    public void Dispose()
    {
    }
}