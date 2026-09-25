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
        private Stash<PlayerInputComponent> _inputStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PlayerInputComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _inputStash = World.GetStash<PlayerInputComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            Vector3 direction = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                0f,
                Input.GetAxisRaw("Vertical")
            );

            bool death = Input.GetButtonDown("Fire1");
            bool victory = Input.GetButtonDown("Fire2");

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            foreach (var entity in _filter)
            {
                ref var movement = ref _movementStash.Get(entity);
                ref var input = ref _inputStash.Get(entity);
                
                movement.Direction = direction;
                input.VictoryPressed = victory;
                input.DeathPressed = death;
            }
        }

        public void Dispose()
        {
        }
    }
}