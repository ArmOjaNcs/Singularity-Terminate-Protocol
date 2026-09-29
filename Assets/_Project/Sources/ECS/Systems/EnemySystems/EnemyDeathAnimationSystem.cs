using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class EnemyDeathAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<DeathComponent> _deathStash;
        private Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private Stash<DeathRequest> _deathRequestStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EnemyTag>()
                .With<DeathComponent>()
                .With<EnemyViewComponent>()
                .Build();

            _deathStash = World.GetStash<DeathComponent>();
            _animatedCreatureStash =
                World.GetStash<AnimatedCreatureComponent>();

            _deathRequestStash = World.GetStash<DeathRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref DeathComponent death =
                    ref _deathStash.Get(entity);

                if (death.AnimationStarted)
                    continue;

                AnimatedCreature view = _animatedCreatureStash.Get(entity).View;

                death.AnimationStarted = true;

                view.OnDeathPerformed += OnDeathPerformed;
                view.SetDeath();
            }
        }

        private void OnDeathPerformed(AnimatedCreature view)
        {
            Entity requestEntity = World.CreateEntity();

            _deathRequestStash.Set(
                requestEntity,
                new DeathRequest
                {
                    Target = view.Entity
                });

            view.OnDeathPerformed -= OnDeathPerformed;
        }

        public void Dispose()
        {
        }
    }
}