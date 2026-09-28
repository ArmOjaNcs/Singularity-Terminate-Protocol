using ECS.CommonComponents;
using Gameplay.Common;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class DeathAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<DeathComponent> _deathStash;
        private Stash<ViewComponent> _viewStash;
        private Stash<DeathRequest> _deathRequestStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<DeathComponent>()
                .With<ViewComponent>()
                .Build();

            _deathStash = World.GetStash<DeathComponent>();
            _viewStash = World.GetStash<ViewComponent>();
            _deathRequestStash = World.GetStash<DeathRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref DeathComponent death = ref _deathStash.Get(entity);

                if (death.AnimationStarted)
                    continue;

                EntityView view = _viewStash.Get(entity).View;

                if (view is not AnimatedCreature creature)
                    continue;

                death.AnimationStarted = true;
                creature.OnDeathPerformed += OnDeathPerformed;
                creature.SetDeath();
            }
        }

        private void OnDeathPerformed(EntityView view)
        {
            Entity requestEntity = World.CreateEntity();

            _deathRequestStash.Set(requestEntity, new DeathRequest{Target = view.Entity});

            if (view is AnimatedCreature creature)
            {
                creature.OnDeathPerformed -= OnDeathPerformed;
            }
        }

        public void Dispose()
        {
        }
    }
}