using ECS.CommonComponents;
using ECS.PlayerComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class PlayerInputSystem : ISystem
    {
        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PlayerInputComponent> _inputStash;
        private Stash<MovementLockComponent> _movementLockStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PlayerInputComponent>()
                .Without<DeathComponent>()
                .Build();

            _movementStash =
                World.GetStash<MovementComponent>();

            _inputStash =
                World.GetStash<PlayerInputComponent>();

            _movementLockStash =
                World.GetStash<MovementLockComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            Vector3 direction = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"),
                0f);

            bool death =
                Input.GetButtonDown("Fire1");

            bool victory =
                Input.GetButtonDown("Fire2");

            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            foreach (var entity in _filter)
            {
                ref var movement =
                    ref _movementStash.Get(entity);

                ref var input =
                    ref _inputStash.Get(entity);

                if (_movementLockStash.Has(entity))
                {
                    movement.Direction = Vector3.zero;
                    continue;
                }

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