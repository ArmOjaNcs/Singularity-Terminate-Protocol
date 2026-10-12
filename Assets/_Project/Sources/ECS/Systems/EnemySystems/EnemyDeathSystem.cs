using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class EnemyDeathSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Filter _weaponFilter;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private Stash<ViewComponent> _viewStash;
        private Stash<CleanupRequest> _cleanupRequestStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EnemyTag>()
                .With<DeathComponent>()
                .With<AnimatedCreatureComponent>()
                .Build();

            _weaponFilter = World.Filter
                .With<WeaponComponent>()
                .Build();

            _deathStash = World.GetStash<DeathComponent>();
            _animatedCreatureStash = World.GetStash<AnimatedCreatureComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _viewStash = World.GetStash<ViewComponent>();
            _cleanupRequestStash = World.GetStash<CleanupRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref DeathComponent death =
                    ref _deathStash.Get(entity);

                if (death.AnimationStarted)
                    continue;

                AnimatedCreature view =
                    _animatedCreatureStash.Get(entity).View;

                if (view == null)
                    continue;

                death.AnimationStarted = true;

                view.OnDeathPerformed -= OnDeathPerformed;
                view.OnDeathPerformed += OnDeathPerformed;

                view.SetDeath();
            }
        }

        private void OnDeathPerformed(AnimatedCreature view)
        {
            view.OnDeathPerformed -= OnDeathPerformed;

            Entity entity = view.Entity;

            if (entity == default)
                return;

            if (_viewStash.Has(entity))
            {
                ViewComponent viewComponent = _viewStash.Get(entity);

                if (viewComponent.View != null)
                    viewComponent.View.SetActive(false);
            }

            Entity request = World.CreateEntity();

            _cleanupRequestStash.Set(request, new CleanupRequest
            {
                Candidate = entity
            });
        }

        public void Dispose() { }
    }
}