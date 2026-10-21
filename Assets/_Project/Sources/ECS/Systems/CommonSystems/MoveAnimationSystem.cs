using ECS.CommonComponents;
using ECS.ViewComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class MoveAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<AnimatedCreatureComponent> _animatedCreatureStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<MovementComponent>()
                .With<AnimatedCreatureComponent>()
                .Without<DeathComponent>()
                .Build();

            _movementStash = World.GetStash<MovementComponent>();
            _animatedCreatureStash =
                World.GetStash<AnimatedCreatureComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref MovementComponent movement =
                    ref _movementStash.Get(entity);

                AnimatedCreature view =
                    _animatedCreatureStash.Get(entity).View;

                if (view == null)
                    continue;

                bool isMoving =
                    movement.Direction.sqrMagnitude > 0f;

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