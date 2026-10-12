using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;

public sealed class ItemScanSystem : ISystem
{
    private Filter _looterFilter;
    private Filter _itemFilter;

    private Stash<PositionComponent> _positionStash;
    private Stash<LooterComponent> _looterStash;
    private Stash<ItemPickupEvent> _pickupEventStash;

    public World World { get; set; }

    public void OnAwake()
    {
        _positionStash = World.GetStash<PositionComponent>();
        _looterStash = World.GetStash<LooterComponent>();
        _pickupEventStash = World.GetStash<ItemPickupEvent>();

        _looterFilter = World.Filter
            .With<PositionComponent>()
            .With<LooterComponent>()
            .Build();

        _itemFilter = World.Filter
            .With<PositionComponent>()
            .With<ItemPickupComponent>()
            .Without<ItemPickupEvent>()
            .Build();
    }

    public void OnUpdate(float deltaTime)
    {
        foreach (var looterEntity in _looterFilter)
        {
            Vector3 looterPos = _positionStash.Get(looterEntity).Position;
            float radius = _looterStash.Get(looterEntity).PickupRadius;
            float sqrRadius = radius * radius;

            foreach (var itemEntity in _itemFilter)
            {
                Vector3 itemPos = _positionStash.Get(itemEntity).Position;

                if ((itemPos - looterPos).sqrMagnitude <= sqrRadius)
                {
                    _pickupEventStash.Set(itemEntity, new ItemPickupEvent
                    {
                        LooterEntity = looterEntity,
                    });
                }
            }
        }
    }

    public void Dispose()
    {
    }
}