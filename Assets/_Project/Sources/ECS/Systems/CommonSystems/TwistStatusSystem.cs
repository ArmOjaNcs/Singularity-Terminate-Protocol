using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class TwistStatusSystem : ISystem
    {
        private const float TwoPi = Mathf.PI * 2f;

        private Filter _filter;
        private Stash<TwistStatusComponent> _twistStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<MovementLockComponent> _movementLockStash;
        private Filter _requestFilter;
        private Stash<TwistStatusRequest> _requestStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<TwistStatusComponent>()
                .With<PositionComponent>()
                .Build();
            _requestFilter = World.Filter.With<TwistStatusRequest>().Build();
            _requestStash = World.GetStash<TwistStatusRequest>();

            _twistStash = World.GetStash<TwistStatusComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            ApplyRequests();
            float frameTime = Mathf.Max(0f, deltaTime);

            foreach (Entity enemy in _filter)
            {
                ref TwistStatusComponent status = ref _twistStash.Get(enemy);
                status.VisualOffset = Vector3.zero;

                if (status.Instances == null || status.Instances.Count == 0)
                    continue;

                if (_deathStash.Has(enemy))
                {
                    status.Instances.Clear();
                    continue;
                }

                // Knockback has higher priority than Twist, so pause the
                // visual reaction and its timer until the movement lock ends.
                if (_movementLockStash.Has(enemy))
                    continue;

                for (int i = status.Instances.Count - 1; i >= 0; i--)
                {
                    TwistInstance instance = status.Instances[i];
                    instance.RemainingDuration -= frameTime;

                    if (instance.RemainingDuration <= 0f)
                    {
                        status.Instances.RemoveAt(i);
                        continue;
                    }

                    instance.Elapsed += frameTime;
                    status.Instances[i] = instance;

                    float phase = instance.Elapsed *
                                  instance.ShakeFrequency * TwoPi +
                                  instance.Phase;
                    float x = Mathf.Sin(phase);
                    float y = Mathf.Sin(phase * 1.37f + 1.1f) * 0.65f;
                    status.VisualOffset += new Vector3(x, y, 0f) *
                                           instance.ShakeAmplitude;
                }
            }
        }

        private void ApplyRequests()
        {
            foreach (Entity requestEntity in _requestFilter)
            {
                TwistStatusRequest request = _requestStash.Get(requestEntity);
                if (request.Target == default || World.IsDisposed(request.Target) ||
                    _deathStash.Has(request.Target) || request.Duration <= 0f ||
                    request.ShakeAmplitude <= 0f || request.ShakeFrequency <= 0f)
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (!_twistStash.Has(request.Target))
                    _twistStash.Set(request.Target, new TwistStatusComponent
                    {
                        Instances = new System.Collections.Generic.List<TwistInstance>()
                    });

                ref TwistStatusComponent status = ref _twistStash.Get(request.Target);
                if (status.Instances == null)
                    status.Instances = new System.Collections.Generic.List<TwistInstance>();
                int index = status.Instances.FindIndex(instance =>
                    instance.SourceWeapon == request.SourceWeapon);
                TwistInstance instance = index >= 0
                    ? status.Instances[index]
                    : new TwistInstance
                    {
                        SourceWeapon = request.SourceWeapon,
                        Phase = Random.Range(0f, Mathf.PI * 2f)
                    };
                instance.RemainingDuration = request.Duration;
                instance.ShakeAmplitude = request.ShakeAmplitude;
                instance.ShakeFrequency = request.ShakeFrequency;
                if (index >= 0)
                    status.Instances[index] = instance;
                else
                    status.Instances.Add(instance);
                World.RemoveEntity(requestEntity);
            }
        }

        public void Dispose() { }
    }
}
