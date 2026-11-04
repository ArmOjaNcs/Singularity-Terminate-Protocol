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

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<TwistStatusComponent>()
                .With<PositionComponent>()
                .Build();

            _twistStash = World.GetStash<TwistStatusComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
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

        public void Dispose() { }
    }
}
