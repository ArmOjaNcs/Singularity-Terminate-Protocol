using ECS.CommonComponents;
using ECS.DamageComponents;
using ECS.EffectComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class DamageSystem : ISystem
    {
        private const float BaseCriticalChance = 0.05f;
        private const float CriticalChancePerLuck = 0.002f;
        private const float MaxCriticalChance = 0.75f;
        private const float CriticalDamageMultiplier = 2f;

        public World World { get; set; }

        private Filter _filter;

        private Stash<DamageRequest> _damageRequestStash;
        private Stash<HealthComponent> _healthStash;
        private Stash<StatsComponent> _statsStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<DamageStatisticComponent> _damageStatisticStash;
        private Stash<EffectRequest> _effectRequestStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<ColorRuntimeComponent> _colorRuntimeStash;

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
            _weaponStash = World.GetStash<WeaponComponent>();
            _colorRuntimeStash = World.GetStash<ColorRuntimeComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _filter)
            {
                ref DamageRequest request =
                    ref _damageRequestStash.Get(requestEntity);

                if (request.Target == default ||
                    _deathStash.Has(request.Target) ||
                    !_healthStash.Has(request.Target))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                float damage = Mathf.Max(0f, request.Damage);
                float blockedDamage = 0f;
                bool isCritical = false;

                if (request.Context.DamageType == DamageType.Direct)
                {
                    damage = CalculateDirectDamage(request, out isCritical);

                    float defence = 0f;

                    if (_statsStash.Has(request.Target))
                        defence = _statsStash.Get(request.Target).Defence;

                    defence = Mathf.Clamp(defence, 0f, 100f);

                    float damageBeforeDefence = damage;

                    damage *= 1f - defence / 100f;

                    blockedDamage = damageBeforeDefence - damage;
                }

                ref HealthComponent health = ref _healthStash.Get(request.Target);

                float currentHealth = health.Current;

                health.Current = Mathf.Max(0f, health.Current - damage);
                float appliedDamage = currentHealth - health.Current;

                if (currentHealth > 0f && health.Current <= 0f)
                    _deathStash.Set(request.Target, new DeathComponent());

                CreateDamageStatistic(request, damage, blockedDamage, isCritical);

                if (request.Context.DamageType == DamageType.Direct && appliedDamage > 0f)
                    AddDirectDamageFlash(request.Target);

                if (request.Context.DamageType == DamageType.Direct)
                    CreateEffectRequest(request);

                World.RemoveEntity(requestEntity);
            }
        }

        private float CalculateDirectDamage(DamageRequest request, out bool isCritical)
        {
            float damage = Mathf.Max(0f, request.Damage);
            float luck = 0f;

            Entity weapon = request.Context.Weapon;

            if (weapon != default && _weaponStash.Has(weapon))
            {
                Entity owner = _weaponStash.Get(weapon).Owner;

                if (owner != default && _statsStash.Has(owner))
                {
                    StatsComponent stats = _statsStash.Get(owner);

                    damage += stats.Damage;
                    luck = Mathf.Max(0f, stats.Luck);
                }
            }

            float criticalChance = Mathf.Clamp(
                BaseCriticalChance + luck * CriticalChancePerLuck,
                BaseCriticalChance,
                MaxCriticalChance);

            isCritical = Random.value < criticalChance;

            if (isCritical)
                damage *= CriticalDamageMultiplier;

            return damage;
        }

        private void AddDirectDamageFlash(Entity target)
        {
            if (!_colorRuntimeStash.Has(target))
            {
                _colorRuntimeStash.Set(target, new ColorRuntimeComponent
                {
                    ActiveColors = new List<ColorRuntime>()
                });
            }

            ref ColorRuntimeComponent runtime = ref _colorRuntimeStash.Get(target);
           
            runtime.ActiveColors.Add(new ColorRuntime
            {
                Reference = Color.red,
                Duration = 0.3f,
                CurrentTime = 0f
            });
        }

        private void CreateDamageStatistic(
            DamageRequest request,
            float damage,
            float blockedDamage,
            bool isCritical)
        {
            Entity statisticEntity = World.CreateEntity();

            _damageStatisticStash.Set(
                statisticEntity,
                new DamageStatisticComponent
                {
                    Target = request.Target,
                    Context = request.Context,
                    Damage = damage,
                    BlockedDamage = blockedDamage,
                    IsCritical = isCritical
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