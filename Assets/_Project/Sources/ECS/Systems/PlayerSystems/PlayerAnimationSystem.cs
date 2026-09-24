using ECS.CommonComponents;
using ECS.PlayerComponents;
using Gameplay.Player;
using Scellecs.Morpeh;

namespace ECS.PlayerSystems
{
    public sealed class PlayerAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<ViewComponent> _viewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<ViewComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _viewStash = World.GetStash<ViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var movement = ref _movementStash.Get(entity);
                ref var viewComponent = ref _viewStash.Get(entity);

                PlayerView view = (PlayerView)viewComponent.View;

                bool isMoving = movement.Direction != UnityEngine.Vector3.zero;

                view.SetMoving(isMoving);

                if (movement.Direction.x > 0f)
                    view.SetFacingLeft(false);
                else if (movement.Direction.x < 0f)
                    view.SetFacingLeft(true);
            }
        }

        public void Dispose()
        {
        }
    }
}