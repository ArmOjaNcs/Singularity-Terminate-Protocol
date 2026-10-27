using ECS.CommonComponents;
using ECS.PlayerComponents;
using Gameplay;
using Gameplay.Navigation;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class PlayerMovementSystem : ISystem
    {
        private readonly MovementResolver _movementResolver;

        private Filter _filter;
        private Stash<MovementComponent> _movementStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<NavigationBoxComponent> _navigationBoxStash;
        private Stash<MovementLockComponent> _movementLockStash;

        public PlayerMovementSystem(NavigationGrid navigationGrid)
        {
            _movementResolver = new MovementResolver(navigationGrid);
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PositionComponent>()
                .With<PlayerStatsComponent>()
                .With<NavigationBoxComponent>()
                .Without<DeathComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _navigationBoxStash = World.GetStash<NavigationBoxComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                if (_movementLockStash.Has(entity))
                    continue;

                ref var movement = ref _movementStash.Get(entity);
                ref var position = ref _positionStash.Get(entity);
                ref var stats = ref _statsStash.Get(entity);
                ref var navigationBox =
                    ref _navigationBoxStash.Get(entity);

                Vector3 delta = movement.Direction * stats.Speed * deltaTime;

                if (delta.sqrMagnitude <= 0f)
                    continue;

                _movementResolver.Move(
                    ref position.Position,
                    delta,
                    navigationBox.Collider);
            }
        }

        public void Dispose()
        {
        }
    }
}
