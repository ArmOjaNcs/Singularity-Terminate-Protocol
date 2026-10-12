using DG.Tweening;
using ECS.PlayerComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;

public sealed class ItemPickupSystem : ISystem
{
    private Filter _filter;
    private Stash<ItemPickupComponent> _itemPickupStash;
    private Stash<ItemPickupEvent> _pickupEventStash;

    private Stash<ItemViewComponent> _itemViewStash;
    private Stash<PlayerViewComponent> _playerViewStash;

    private Stash<AddExperienceEvent> _addXpStash;

    public World World { get; set; }

    public void OnAwake()
    {
        _itemPickupStash = World.GetStash<ItemPickupComponent>();
        _pickupEventStash = World.GetStash<ItemPickupEvent>();
        _addXpStash = World.GetStash<AddExperienceEvent>();
        _playerViewStash = World.GetStash<PlayerViewComponent>();
        _itemViewStash = World.GetStash<ItemViewComponent>();

        _filter = World.Filter
            .With<ItemPickupComponent>()
            .With<ItemPickupEvent>()
            .Build();
    }

    public void OnUpdate(float deltaTime)
    {
        foreach (var itemEntity in _filter)
        {
            ref var item = ref _itemPickupStash.Get(itemEntity);
            ref var pickupEvent = ref _pickupEventStash.Get(itemEntity);

            Entity looter = pickupEvent.LooterEntity;

            if (World.IsDisposed(looter) || World.IsDisposed(itemEntity) || item.IsPickedUp == true)
            {
                _pickupEventStash.Remove(itemEntity);
                continue;
            }

            item.IsPickedUp = true;

            AnimatePickup(itemEntity, looter);
        }
    }

    public void Dispose()
    {
    }

    private void AnimatePickup(Entity item, Entity looter)
    {
        if (_playerViewStash.Has(looter) == false || World.IsDisposed(item))
            return;

        int xpAmount = _addXpStash.Has(item) ? _addXpStash.Get(item).Amount : 0;
        Transform targetTransform = _playerViewStash.Get(looter).View.transform;
        Vector3 targetPosition = targetTransform.position;
        Transform itemTransform = _itemViewStash.Get(item).View.transform;
        Vector3 itemPosition = itemTransform.position;
        Vector3 bounceDirection = targetTransform.right;

        bounceDirection.y = 0.6f;

        float bounceDistance = 1.5f;
        Vector3 bounceTarget = itemTransform.position + (bounceDirection * bounceDistance);

        itemTransform.DOMove(bounceTarget, 0.25f)
            .SetTarget(itemTransform)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                float progress = 0f;
                Vector3 startPosition = itemTransform.position;
                Tweener flyTween = DOTween.To(() => progress, x => progress = x, 1f, 0.4f)
                                    .SetTarget(itemTransform)
                                    .SetEase(Ease.InQuad);

                flyTween.OnUpdate(() =>
                {
                    if (targetTransform != null && flyTween != null)
                        itemTransform.position = Vector3.Lerp(startPosition, targetTransform.position, progress);
                });

                itemTransform.DOScale(Vector3.zero, 0.4f)
                    .SetTarget(itemTransform)
                    .SetEase(Ease.InQuad);

                flyTween.OnComplete(() =>
                {
                    if (itemTransform != null)
                        Object.Destroy(itemTransform.gameObject);

                    if (World.IsDisposed(looter) == false && xpAmount > 0)
                    {
                        Entity xpEventEntity = World.CreateEntity();

                        _addXpStash.Set(xpEventEntity, new AddExperienceEvent
                        {
                            LooterEntity = looter,
                            Amount = xpAmount,
                        });
                    }

                    if (World.IsDisposed(item) == false)
                        World.RemoveEntity(item);
                });
            });
    }
}