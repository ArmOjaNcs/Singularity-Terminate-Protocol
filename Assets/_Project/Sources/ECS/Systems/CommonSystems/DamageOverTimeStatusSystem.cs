using ECS.CommonComponents;
using ECS.DamageComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class DamageOverTimeStatusSystem : ISystem
    {
        private const float TickInterval = 1f;

        private Filter _filter;
        private Stash<DamageOverTimeStatusComponent> _statusStash;
        private Stash<DamageRequest> _damageRequestStash;
        private Stash<DeathComponent> _deathStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter.With<DamageOverTimeStatusComponent>().Build();
            _statusStash = World.GetStash<DamageOverTimeStatusComponent>();
            _damageRequestStash = World.GetStash<DamageRequest>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity target in _filter)
            {
                ref DamageOverTimeStatusComponent status = ref _statusStash.Get(target);
                if (status.Instances == null || status.Instances.Count == 0)
                    continue;
                if (_deathStash.Has(target))
                {
                    status.Instances.Clear();
                    continue;
                }

                for (int i = status.Instances.Count - 1; i >= 0; i--)
                {
                    DamageOverTimeInstance instance = status.Instances[i];
                    float activeTime = Mathf.Min(Mathf.Max(0f, deltaTime), Mathf.Max(0f, instance.RemainingDuration));
                    float timeToProcess = activeTime;
                    while (timeToProcess > 0f)
                    {
                        float step = Mathf.Min(timeToProcess, TickInterval - instance.TickElapsed);
                        instance.PendingDamage += instance.DamagePerStackPerSecond * instance.Stacks * step;
                        instance.TickElapsed += step;
                        timeToProcess -= step;
                        if (instance.TickElapsed >= TickInterval)
                        {
                            CreateDamageRequest(target, instance, instance.PendingDamage);
                            instance.PendingDamage = 0f;
                            instance.TickElapsed = 0f;
                        }
                    }

                    instance.RemainingDuration -= deltaTime;
                    if (instance.RemainingDuration <= 0f)
                    {
                        if (instance.PendingDamage > 0f)
                            CreateDamageRequest(target, instance, instance.PendingDamage);
                        status.Instances.RemoveAt(i);
                    }
                    else
                    {
                        status.Instances[i] = instance;
                    }
                }
            }
        }

        private void CreateDamageRequest(Entity target, DamageOverTimeInstance instance, float damage)
        {
            if (damage <= 0f)
                return;
            DamageContext context = instance.SourceContext;
            context.DamageType = DamageType.Effect;
            context.EffectType = instance.EffectType;
            Entity request = World.CreateEntity();
            _damageRequestStash.Set(request, new DamageRequest
            {
                Target = target,
                Damage = damage,
                Context = context,
                ApplyTwistOnDamage = instance.ApplyTwistOnDamage,
                TwistShakeAmplitude = instance.TwistShakeAmplitude,
                TwistShakeFrequency = instance.TwistShakeFrequency,
                TwistDuration = instance.TwistDuration
            });
        }

        public void Dispose() { }
    }
}
