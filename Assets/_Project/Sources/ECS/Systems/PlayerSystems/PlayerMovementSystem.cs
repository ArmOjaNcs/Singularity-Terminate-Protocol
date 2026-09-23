using ECS.CommonComponents;
using ECS.PlayerComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class PlayerMovementSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<PlayerStatsComponent> _statsStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PositionComponent>()
                .With<PlayerStatsComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _statsStash = World.GetStash<PlayerStatsComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var movement = ref _movementStash.Get(entity);
                ref var position = ref _positionStash.Get(entity);
                ref var stats = ref _statsStash.Get(entity);

                position.Position += movement.Direction * stats.Stats.Speed * deltaTime;
            }
        }

        public void Dispose()
        {
        }
    }
}