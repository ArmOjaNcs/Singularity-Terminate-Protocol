using ECS.CommonComponents;
using ECS.EffectComponents;
using ECS.EnemyComponents;
using ECS.WeaponComponents;
using EffectComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class EffectSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<EffectRequest> _effectRequestStash;
        private Stash<WeaponEffectsComponent> _weaponEffectsStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<KnockbackRequest> _knockbackRequestStash;
        private Stash<TraumaStatusComponent> _traumaStatusStash;
        private Stash<DamageOverTimeStatusComponent> _damageOverTimeStatusStash;
        private Stash<BurnStatusComponent> _burnStatusStash;
        private Stash<TwistStatusComponent> _twistStatusStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<DeathComponent> _deathStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EffectRequest>()
                .Build();

            _effectRequestStash =
                World.GetStash<EffectRequest>();

            _weaponEffectsStash =
                World.GetStash<WeaponEffectsComponent>();

            _weaponStash =
                World.GetStash<WeaponComponent>();

            _positionStash =
                World.GetStash<PositionComponent>();

            _knockbackRequestStash =
                World.GetStash<KnockbackRequest>();
            _traumaStatusStash = World.GetStash<TraumaStatusComponent>();
            _damageOverTimeStatusStash = World.GetStash<DamageOverTimeStatusComponent>();
            _burnStatusStash = World.GetStash<BurnStatusComponent>();
            _twistStatusStash = World.GetStash<TwistStatusComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _filter)
            {
                ref EffectRequest request =
                    ref _effectRequestStash.Get(requestEntity);

                Entity weapon =
                    request.Context.Weapon;

                if (weapon == default)
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (!_weaponEffectsStash.Has(weapon))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                EffectCollection effects = _weaponEffectsStash.Get(weapon).Effects;

                if (effects.TryGet<KnockbackEffectRuntime>(
                        out KnockbackEffectRuntime knockback))
                {
                    CreateKnockbackRequest(request, knockback);
                }

                if (request.AppliedDamage > 0f &&
                    _enemyTagStash.Has(request.Target) &&
                    !_deathStash.Has(request.Target) &&
                    effects.TryGet<TraumaEffectRuntime>(
                        out TraumaEffectRuntime trauma))
                {
                    ApplyTrauma(request.Target, weapon, trauma);
                }

                if (request.AppliedDamage > 0f &&
                    _enemyTagStash.Has(request.Target) &&
                    !_deathStash.Has(request.Target) &&
                    effects.TryGet<BleedEffectRuntime>(
                        out BleedEffectRuntime bleed))
                {
                    ApplyDamageOverTime(request.Target, weapon, request.Context,
                        EffectType.Bleed, bleed.MaxStacks,
                        bleed.DamagePerStackPerSecond, bleed.Duration, null);
                }

                if (request.AppliedDamage > 0f &&
                    _enemyTagStash.Has(request.Target) &&
                    !_deathStash.Has(request.Target) &&
                    effects.TryGet<ShockEffectRuntime>(out ShockEffectRuntime shock))
                {
                    ApplyDamageOverTime(request.Target, weapon, request.Context,
                        EffectType.Shock, shock.MaxStacks,
                        shock.DamagePerStackPerSecond, shock.Duration,
                        shock.TwistOnDamage);
                }

                if (request.AppliedDamage > 0f &&
                    _enemyTagStash.Has(request.Target) &&
                    !_deathStash.Has(request.Target) &&
                    effects.TryGet<BurnEffectRuntime>(
                        out BurnEffectRuntime burn))
                {
                    ApplyBurn(request.Target, weapon, request.Context, burn);
                }

                if (request.AppliedDamage > 0f &&
                    _enemyTagStash.Has(request.Target) &&
                    !_deathStash.Has(request.Target) &&
                    effects.TryGet<TwistEffectRuntime>(
                        out TwistEffectRuntime twist))
                {
                    ApplyTwist(request.Target, weapon, twist);
                }

                World.RemoveEntity(requestEntity);
            }
        }

        private void ApplyDamageOverTime(
            Entity target,
            Entity sourceWeapon,
            DamageContext sourceContext,
            EffectType effectType,
            int maxStacks,
            float damagePerStackPerSecond,
            float duration,
            TwistEffectRuntime twistOnDamage)
        {
            maxStacks = Mathf.Max(1, maxStacks);
            if (duration <= 0f || damagePerStackPerSecond <= 0f)
                return;

            if (!_damageOverTimeStatusStash.Has(target))
            {
                _damageOverTimeStatusStash.Set(target, new DamageOverTimeStatusComponent
                {
                    Instances = new System.Collections.Generic.List<DamageOverTimeInstance>()
                });
            }

            ref DamageOverTimeStatusComponent status = ref _damageOverTimeStatusStash.Get(target);
            if (status.Instances == null)
                status.Instances = new System.Collections.Generic.List<DamageOverTimeInstance>();

            int index = status.Instances.FindIndex(instance =>
                instance.SourceWeapon == sourceWeapon && instance.EffectType == effectType);
            DamageOverTimeInstance instance = index >= 0
                ? status.Instances[index]
                : new DamageOverTimeInstance { SourceWeapon = sourceWeapon };

            instance.EffectType = effectType;
            instance.MaxStacks = maxStacks;
            instance.Stacks = Mathf.Min(maxStacks, instance.Stacks + 1);
            instance.DamagePerStackPerSecond = damagePerStackPerSecond;
            instance.RemainingDuration = duration;
            instance.SourceContext = sourceContext;
            instance.ApplyTwistOnDamage = effectType == EffectType.Shock && twistOnDamage != null;
            if (twistOnDamage != null)
            {
                instance.TwistShakeAmplitude = twistOnDamage.ShakeAmplitude;
                instance.TwistShakeFrequency = twistOnDamage.ShakeFrequency;
                instance.TwistDuration = twistOnDamage.RefreshDuration;
            }

            if (index >= 0)
                status.Instances[index] = instance;
            else
                status.Instances.Add(instance);
        }

        private void ApplyBurn(
            Entity target,
            Entity sourceWeapon,
            DamageContext sourceContext,
            BurnEffectRuntime burn)
        {
            int maxStacks = Mathf.Max(1, burn.MaxStacks);
            if (burn.Duration <= 0f ||
                burn.DamagePerStackPerSecond <= 0f)
            {
                return;
            }

            if (!_burnStatusStash.Has(target))
            {
                _burnStatusStash.Set(target, new BurnStatusComponent
                {
                    Instances = new System.Collections.Generic.List<BurnInstance>()
                });
            }

            ref BurnStatusComponent status = ref _burnStatusStash.Get(target);
            if (status.Instances == null)
                status.Instances = new System.Collections.Generic.List<BurnInstance>();

            int index = status.Instances.FindIndex(
                instance => instance.SourceWeapon == sourceWeapon);

            BurnInstance instance = index >= 0
                ? status.Instances[index]
                : new BurnInstance
                {
                    SourceWeapon = sourceWeapon,
                    Stacks = 0,
                    TickElapsed = 0f,
                    PendingDamage = 0f,
                    PanicCooldownRemaining = GetPanicInterval(burn, 1, maxStacks)
                };

            instance.MaxStacks = maxStacks;
            instance.Stacks = Mathf.Min(maxStacks, instance.Stacks + 1);
            instance.DamagePerStackPerSecond =
                burn.DamagePerStackPerSecond;
            instance.RemainingDuration = burn.Duration;
            instance.BasePanicInterval = Mathf.Max(0.1f, burn.BasePanicInterval);
            instance.MinPanicInterval = Mathf.Clamp(
                burn.MinPanicInterval,
                0.1f,
                instance.BasePanicInterval);
            instance.PanicDistance = Mathf.Max(0f, burn.PanicDistance);
            instance.PanicSpeedMultiplier = Mathf.Max(1f, burn.PanicSpeedMultiplier);
            instance.PanicBurstDuration = Mathf.Max(0.1f, burn.PanicBurstDuration);
            instance.PanicCooldownRemaining = Mathf.Min(
                instance.PanicCooldownRemaining,
                GetPanicInterval(burn, instance.Stacks, maxStacks));
            instance.SourceContext = sourceContext;

            if (index >= 0)
                status.Instances[index] = instance;
            else
                status.Instances.Add(instance);
        }

        private void ApplyTwist(
            Entity target,
            Entity sourceWeapon,
            TwistEffectRuntime twist)
        {
            if (twist.RefreshDuration <= 0f ||
                twist.ShakeAmplitude <= 0f ||
                twist.ShakeFrequency <= 0f)
            {
                return;
            }

            if (!_twistStatusStash.Has(target))
            {
                _twistStatusStash.Set(target, new TwistStatusComponent
                {
                    Instances = new System.Collections.Generic.List<TwistInstance>()
                });
            }

            ref TwistStatusComponent status =
                ref _twistStatusStash.Get(target);
            if (status.Instances == null)
                status.Instances = new System.Collections.Generic.List<TwistInstance>();

            int index = status.Instances.FindIndex(
                instance => instance.SourceWeapon == sourceWeapon);

            TwistInstance instance = index >= 0
                ? status.Instances[index]
                : new TwistInstance
                {
                    SourceWeapon = sourceWeapon,
                    Phase = Random.Range(0f, Mathf.PI * 2f)
                };

            instance.RemainingDuration = twist.RefreshDuration;
            instance.ShakeAmplitude = Mathf.Max(0f, twist.ShakeAmplitude);
            instance.ShakeFrequency = Mathf.Max(0.1f, twist.ShakeFrequency);

            if (index >= 0)
                status.Instances[index] = instance;
            else
                status.Instances.Add(instance);
        }

        private static float GetPanicInterval(
            BurnEffectRuntime burn,
            int stacks,
            int maxStacks)
        {
            float baseInterval = Mathf.Max(0.1f, burn.BasePanicInterval);
            float minInterval = Mathf.Clamp(
                burn.MinPanicInterval,
                0.1f,
                baseInterval);
            float stackProgress = maxStacks <= 1
                ? 1f
                : Mathf.Clamp01((stacks - 1f) / (maxStacks - 1f));

            return Mathf.Lerp(baseInterval, minInterval, stackProgress);
        }

        private void ApplyTrauma(
            Entity target,
            Entity sourceWeapon,
            TraumaEffectRuntime trauma)
        {
            if (trauma.Duration <= 0f)
                return;

            if (!_traumaStatusStash.Has(target))
            {
                _traumaStatusStash.Set(target, new TraumaStatusComponent
                {
                    Instances = new System.Collections.Generic.List<TraumaInstance>()
                });
            }

            ref TraumaStatusComponent status =
                ref _traumaStatusStash.Get(target);

            if (status.Instances == null)
                status.Instances = new System.Collections.Generic.List<TraumaInstance>();

            int index = status.Instances.FindIndex(
                instance => instance.SourceWeapon == sourceWeapon);

            TraumaInstance instance = index >= 0
                ? status.Instances[index]
                : new TraumaInstance { SourceWeapon = sourceWeapon };

            instance.RemainingDuration = trauma.Duration;
            instance.MovementSlowPercent = Mathf.Max(
                instance.MovementSlowPercent,
                Mathf.Clamp(trauma.MovementSlowPercent, 0f, 100f));
            instance.AttackDamageReductionPercent = Mathf.Max(
                instance.AttackDamageReductionPercent,
                Mathf.Clamp(trauma.AttackDamageReductionPercent, 0f, 100f));

            if (index >= 0)
                status.Instances[index] = instance;
            else
                status.Instances.Add(instance);
        }

        private void CreateKnockbackRequest(
            EffectRequest request,
            KnockbackEffectRuntime knockback)
        {
            Entity weapon =
                request.Context.Weapon;

            if (!_weaponStash.Has(weapon))
                return;

            Entity owner =
                _weaponStash.Get(weapon).Owner;

            if (!_positionStash.Has(owner) ||
                !_positionStash.Has(request.Target))
                return;

            Vector3 sourcePosition =
                _positionStash.Get(owner).Position;

            Vector3 targetPosition =
                _positionStash.Get(request.Target).Position;

            Vector3 direction =
                targetPosition - sourcePosition;

            direction.z = 0f;

            if (direction.sqrMagnitude <= 0f)
                return;

            direction.Normalize();

            Entity knockbackRequestEntity =
                World.CreateEntity();

            _knockbackRequestStash.Set(
                knockbackRequestEntity,
                new KnockbackRequest
                {
                    Source = owner,
                    Target = request.Target,
                    Direction = direction,
                    Force = knockback.Force
                });
        }

        public void Dispose() { }
    }
}
