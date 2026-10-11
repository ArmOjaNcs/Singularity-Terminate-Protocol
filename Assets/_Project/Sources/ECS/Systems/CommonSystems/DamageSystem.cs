using ECS.CommonComponents;
using ECS.DamageComponents;
using ECS.EffectComponents;
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
        private Stash<DamageStatisticComponent> _damageStatisticStash;
        private Stash<EffectRequest> _effectRequestStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<DamageRequest>()
                .Build();

            _damageRequestStash = World.GetStash<DamageRequest>();
            _healthStash = World.GetStash<HealthComponent>();
            _statsStash = World.GetStash<StatsComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _damageStatisticStash = World.GetStash<DamageStatisticComponent>();
            _effectRequestStash = World.GetStash<EffectRequest>();
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
                    defence = _statsStash.Get(request.Target).Defence;

                float blockedDamage = Mathf.Min(request.Damage, defence);
                float damage = Mathf.Max(0f, request.Damage - defence);

                ref HealthComponent health = ref _healthStash.Get(request.Target);

                float currentHealth = health.Current;

                health.Current = Mathf.Max(0f, health.Current - damage);

                if (currentHealth > 0f && health.Current <= 0f)
                    _deathStash.Set(request.Target, new DeathComponent());

                CreateDamageStatistic(request, damage, blockedDamage);

                if (request.Context.DamageType == DamageType.Direct)
                    CreateEffectRequest(request);

                World.RemoveEntity(requestEntity);
            }
        }

        private void CreateDamageStatistic(DamageRequest request, float damage, float blockedDamage)
        {
            Entity statisticEntity = World.CreateEntity();

            _damageStatisticStash.Set(
                statisticEntity,
                new DamageStatisticComponent
                {
                    Target = request.Target,
                    Context = request.Context,
                    Damage = damage,
                    BlockedDamage = blockedDamage
                });
        }

        private void CreateEffectRequest(DamageRequest request)
        {
            Entity effectRequestEntity = World.CreateEntity();

            _effectRequestStash.Set(
                effectRequestEntity,
                new EffectRequest
                {
                    Target = request.Target,
                    Context = request.Context
                });
        }

        public void Dispose() { }
    }
}