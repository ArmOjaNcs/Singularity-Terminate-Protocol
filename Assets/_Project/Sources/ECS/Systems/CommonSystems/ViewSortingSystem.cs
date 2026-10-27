using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.ViewComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class ViewSortingSystem : ISystem
    {
        private const int SortingMultiplier = 100;

        private Filter _filter;
        private Stash<PositionComponent> _positionStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<NavigationBoxComponent> _navigationBoxStash;
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
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _navigationBoxStash = World.GetStash<NavigationBoxComponent>();
            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position = ref _positionStash.Get(entity);
                ref ViewComponent viewComponent = ref _viewStash.Get(entity);

                float colliderBottomY = position.Position.y;

                bool hasSortingCollider = false;
                ColliderComponent sortingCollider = default;

                if (_navigationBoxStash.Has(entity))
                {
                    sortingCollider =
                        _navigationBoxStash.Get(entity).Collider;
                    hasSortingCollider = true;
                }
                else if (_hitBoxStash.Has(entity))
                {
                    sortingCollider = _hitBoxStash.Get(entity).Collider;
                    hasSortingCollider = true;
                }

                if (hasSortingCollider)
                {
                    Vector3 colliderPosition =
                        ColliderGeometry.GetColliderWorldCenter(
                            position.Position,
                            sortingCollider);
                    colliderBottomY = colliderPosition.y -
                                      GetVerticalExtent(sortingCollider);
                }

                int sortingOrder = -Mathf.RoundToInt(
                    colliderBottomY * SortingMultiplier);

                viewComponent.View.SetSortingOrder(sortingOrder);
            }
        }

        private static float GetVerticalExtent(ColliderComponent collider)
        {
            switch (collider.Type)
            {
                case ColliderType.Circle:
                    return Mathf.Max(0f, collider.Radius);

                case ColliderType.Sector:
                {
                    float radius = Mathf.Max(0f, collider.Radius);
                    float halfAngle = Mathf.Clamp(
                        collider.Size.x * 0.5f,
                        0f,
                        90f);
                    Vector2 direction = new Vector2(
                        Mathf.Cos(collider.Angle * Mathf.Deg2Rad),
                        Mathf.Sin(collider.Angle * Mathf.Deg2Rad));

                    if (Mathf.Abs(Vector2.SignedAngle(
                            direction,
                            Vector2.down)) <= halfAngle)
                    {
                        return radius;
                    }

                    float leftAngle =
                        (collider.Angle - halfAngle) * Mathf.Deg2Rad;
                    float rightAngle =
                        (collider.Angle + halfAngle) * Mathf.Deg2Rad;
                    float lowestArcY = Mathf.Min(
                        Mathf.Sin(leftAngle),
                        Mathf.Sin(rightAngle)) * radius;

                    return Mathf.Max(0f, -lowestArcY);
                }

                case ColliderType.Box:
                {
                    float angle = collider.Angle * Mathf.Deg2Rad;
                    float halfHeight =
                        Mathf.Abs(Mathf.Sin(angle)) *
                        Mathf.Max(0f, collider.Size.x) * 0.5f +
                        Mathf.Abs(Mathf.Cos(angle)) *
                        Mathf.Max(0f, collider.Size.y) * 0.5f;

                    return halfHeight;
                }

                case ColliderType.Capsule:
                {
                    float radius = Mathf.Max(0f, collider.Radius);
                    float halfSegmentLength = Mathf.Max(
                        0f,
                        Mathf.Max(0f, collider.Length) * 0.5f - radius);
                    float axisAngle = collider.Angle +
                                      (collider.CapsuleOrientation ==
                                       CapsuleOrientation.Y
                                          ? 90f
                                          : 0f);

                    return radius + halfSegmentLength *
                           Mathf.Abs(Mathf.Sin(axisAngle * Mathf.Deg2Rad));
                }

                default:
                    return 0f;
            }
        }

        public void Dispose()
        {
        }
    }
}
