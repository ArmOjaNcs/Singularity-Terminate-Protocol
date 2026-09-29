using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class DamageSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<DamageRequest> _damageRequestStash;
        private Stash<HealthComponent> _healthStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<DeathComponent> _deathStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<DamageRequest>()
                .Build();

            _damageRequestStash = World.GetStash<DamageRequest>();
            _healthStash = World.GetStash<HealthComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _filter)
            {
                ref DamageRequest request = ref _damageRequestStash.Get(requestEntity);

                if (request.Target == default)
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (_deathStash.Has(request.Target))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (!_healthStash.Has(request.Target))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                float defence = 0f;

                if (_statsStash.Has(request.Target))
                {
                    ref StatsComponent stats = ref _statsStash.Get(request.Target);
                    defence = stats.Defence;
                }

                float damage = Mathf.Max(0f, request.Damage - defence);
                ref HealthComponent health = ref _healthStash.Get(request.Target);
                health.Current -= damage;

                if (health.Current <= 0f && !_deathStash.Has(request.Target))
                {
                    _deathStash.Add(request.Target);
                    ref DeathComponent death = ref _deathStash.Get(request.Target);
                    death.AnimationStarted = false;
                }

                World.RemoveEntity(requestEntity);
            }
        }

        public void Dispose()
        {
        }
    }
}