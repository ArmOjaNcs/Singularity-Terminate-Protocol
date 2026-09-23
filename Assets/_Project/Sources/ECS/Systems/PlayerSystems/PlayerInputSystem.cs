using ECS.CommonComponents;
using ECS.PlayerComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class PlayerInputSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Stash<MovementComponent> _movementStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            Vector3 direction = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                0f,
                Input.GetAxisRaw("Vertical")
            );

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            foreach (var entity in _filter)
            {
                ref var movement = ref _movementStash.Get(entity);
                movement.Direction = direction;
            }
        }

        public void Dispose()
        {
        }
    }
}