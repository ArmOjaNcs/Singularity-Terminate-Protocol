using ECS.CommonComponents;
using ECS.DamageComponents;
using ECS.EnemyComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class BurnStatusSystem : ISystem
    {
        private const float TickInterval = 1f;

        public World World { get; set; }

        private Filter _filter;
        private Stash<BurnStatusComponent> _burnStatusStash;
        private Stash<DamageRequest> _damageRequestStash;
        private Stash<PanicRequest> _panicRequestStash;
        private Stash<DeathComponent> _deathStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BurnStatusComponent>()
                .Build();

            _burnStatusStash = World.GetStash<BurnStatusComponent>();
            _damageRequestStash = World.GetStash<DamageRequest>();
            _panicRequestStash = World.GetStash<PanicRequest>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            float frameTime = Mathf.Max(0f, deltaTime);

            foreach (Entity target in _filter)
            {
                ref BurnStatusComponent status =
                    ref _burnStatusStash.Get(target);

                if (status.Instances == null || status.Instances.Count == 0)
                    continue;

                if (_deathStash.Has(target))
                {
                    status.Instances.Clear();
                    continue;
                }

                for (int i = status.Instances.Count - 1; i >= 0; i--)
                {
                    BurnInstance instance = status.Instances[i];
                    float activeTime = Mathf.Min(
                        frameTime,
                        Mathf.Max(0f, instance.RemainingDuration));

                    ProcessDamage(target, ref instance, activeTime);
                    ProcessPanic(target, ref instance, activeTime);
                    instance.RemainingDuration -= frameTime;

                    if (instance.RemainingDuration <= 0f)
                    {
                        if (instance.PendingDamage > 0f)
                        {
                            CreateDamageRequest(
                                target,
                                instance,
                                instance.PendingDamage);
                        }

                        status.Instances.RemoveAt(i);
                        continue;
                    }

                    status.Instances[i] = instance;
                }
            }
        }

        private void ProcessDamage(
            Entity target,
            ref BurnInstance instance,
            float activeTime)
        {
            float timeToProcess = activeTime;

            while (timeToProcess > 0f)
            {
                float timeToNextTick = TickInterval - instance.TickElapsed;
                float step = Mathf.Min(timeToProcess, timeToNextTick);

                instance.PendingDamage +=
                    instance.DamagePerStackPerSecond *
                    instance.Stacks *
                    step;
                instance.TickElapsed += step;
                timeToProcess -= step;

                if (instance.TickElapsed >= TickInterval)
                {
                    CreateDamageRequest(target, instance, instance.PendingDamage);
                    instance.PendingDamage = 0f;
                    instance.TickElapsed = 0f;
                }
            }
        }

        private void ProcessPanic(
            Entity target,
            ref BurnInstance instance,
            float activeTime)
        {
            if (instance.PanicDistance <= 0f)
                return;

            instance.PanicCooldownRemaining -= activeTime;
            if (instance.PanicCooldownRemaining > 0f)
                return;

            Entity requestEntity = World.CreateEntity();
            _panicRequestStash.Set(requestEntity, new PanicRequest
            {
                Target = target,
                Distance = instance.PanicDistance,
                SpeedMultiplier = instance.PanicSpeedMultiplier,
                Duration = instance.PanicBurstDuration
            });

            instance.PanicCooldownRemaining = GetPanicInterval(instance);
        }

        private void CreateDamageRequest(
            Entity target,
            BurnInstance instance,
            float damage)
        {
            if (damage <= 0f)
                return;

            DamageContext context = instance.SourceContext;
            context.DamageType = DamageType.Effect;
            context.EffectType = EffectType.Burn;

            Entity request = World.CreateEntity();
            _damageRequestStash.Set(request, new DamageRequest
            {
                Target = target,
                Damage = damage,
                Context = context
            });
        }

        private static float GetPanicInterval(BurnInstance instance)
        {
            float baseInterval = Mathf.Max(0.1f, instance.BasePanicInterval);
            float minInterval = Mathf.Clamp(
                instance.MinPanicInterval,
                0.1f,
                baseInterval);
            float stackProgress = instance.MaxStacks <= 1
                ? 1f
                : Mathf.Clamp01(
                    (instance.Stacks - 1f) / (instance.MaxStacks - 1f));

            return Mathf.Lerp(baseInterval, minInterval, stackProgress);
        }

        public void Dispose() { }
    }
}
