using ECS.CommonComponents;
using ECS.PlayerComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.PlayerSystems
{
    public sealed class PlayerAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PlayerInputComponent> _inputStash;
        private Stash<PlayerViewComponent> _playerViewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PlayerInputComponent>()
                .With<PlayerViewComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _inputStash = World.GetStash<PlayerInputComponent>();
            _playerViewStash = World.GetStash<PlayerViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref MovementComponent movement =
                    ref _movementStash.Get(entity);

                ref PlayerInputComponent input =
                    ref _inputStash.Get(entity);

                AnimatedCreature view = _playerViewStash.Get(entity).View;

                bool isMoving =
                    movement.Direction != UnityEngine.Vector3.zero;

                view.SetMoving(isMoving);

                if (movement.Direction.x > 0f)
                    view.SetFacingLeft(false);
                else if (movement.Direction.x < 0f)
                    view.SetFacingLeft(true);

                if (input.VictoryPressed)
                    view.SetWin();
            }
        }

        public void Dispose()
        {
        }
    }
}