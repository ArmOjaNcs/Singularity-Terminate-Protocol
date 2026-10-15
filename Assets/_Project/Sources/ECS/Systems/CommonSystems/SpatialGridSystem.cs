using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class SpatialGridSystem : ISystem
    {
        private readonly SpatialGrid _grid;

        private Filter _filter;

        private Stash<PositionComponent> _positionStash;
        private Stash<ColliderComponent> _colliderStash;
        private Stash<ColliderPositionComponent> _colliderPositionStash;

        public SpatialGridSystem(SpatialGrid grid)
        {
            _grid = grid;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();
            _colliderStash = World.GetStash<ColliderComponent>();
            _colliderPositionStash =
                World.GetStash<ColliderPositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            _grid.Clear();

            foreach (Entity entity in _filter)
            {
                Vector3 position = _positionStash.Get(entity).Position;

                if (_colliderPositionStash.Has(entity))
                {
                    position =
                        _colliderPositionStash.Get(entity).CenterPosition;
                }

                float boundsRadius = 0f;

                if (_colliderStash.Has(entity))
                {
                    ref ColliderComponent collider =
                        ref _colliderStash.Get(entity);

                    boundsRadius = GetBoundsRadius(collider);
                }

                _grid.Add(entity, position, boundsRadius);
            }
        }

        private float GetBoundsRadius(ColliderComponent collider)
        {
            switch (collider.Type)
            {
                case ColliderType.Circle:
                case ColliderType.Sector:
                    return collider.Radius;

                case ColliderType.Capsule:
                    return Mathf.Max(
                        collider.Radius,
                        collider.Length * 0.5f);

                case ColliderType.Box:
                    return collider.Size.magnitude * 0.5f;

                default:
                    return 0f;
            }
        }

        public void Dispose()
        {
        }
    }
}