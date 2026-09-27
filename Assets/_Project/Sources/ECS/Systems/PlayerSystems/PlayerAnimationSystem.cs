using ECS.CommonComponents;
using ECS.PlayerComponents;
using Gameplay.Common;
using Scellecs.Morpeh;

namespace ECS.PlayerSystems
{
    public sealed class PlayerAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PlayerInputComponent> _inputStash;
        private Stash<ViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PlayerInputComponent>()
                .With<ViewComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _inputStash = World.GetStash<PlayerInputComponent>();
            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref MovementComponent movement = ref _movementStash.Get(entity);
                ref PlayerInputComponent input = ref _inputStash.Get(entity);
                ref ViewComponent viewComponent = ref _viewStash.Get(entity);

                if (viewComponent.View is not AnimatedCreature view)
                    continue;

                bool isMoving = movement.Direction != UnityEngine.Vector3.zero;
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