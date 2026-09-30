using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.ViewComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.PlayerSystems
{
    public sealed class PlayerDeathSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<DeathComponent> _deathStash;
        private Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private Stash<HealthComponent> _healthStash;
        private Stash<PlayerStatsComponent> _statsStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<DeathComponent>()
                .With<AnimatedCreatureComponent>()
                .Build();

            _deathStash = World.GetStash<DeathComponent>();
            _animatedCreatureStash =
                World.GetStash<AnimatedCreatureComponent>();
            _healthStash = World.GetStash<HealthComponent>();
            _statsStash = World.GetStash<PlayerStatsComponent>();
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

                view.OnDeathPerformed -= OnDeathPerformed;
                view.OnDeathPerformed += OnDeathPerformed;

                view.SetDeath();
            }
        }

        public void Revive()
        {
            foreach (Entity entity in _filter)
            {
                Revive(entity);
            }
        }

        private void Revive(Entity entity)
        {
            if (!_deathStash.Has(entity))
                return;

            ref HealthComponent health =
                ref _healthStash.Get(entity);

            ref PlayerStatsComponent stats =
                ref _statsStash.Get(entity);

            AnimatedCreature view =
                _animatedCreatureStash.Get(entity).View;

            health.Current = stats.Stats.MaxHealth;

            view.OnDeathPerformed -= OnDeathPerformed;
            view.ResetState();
            view.ResetAnimator();

            _deathStash.Remove(entity);
        }

        private void OnDeathPerformed(AnimatedCreature view)
        {
            view.OnDeathPerformed -= OnDeathPerformed;
        }

        public void Dispose()
        {
        }
    }
}