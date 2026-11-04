using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class TraumaStatusSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Stash<TraumaStatusComponent> _traumaStatusStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<TraumaStatusComponent>()
                .Build();
            _traumaStatusStash = World.GetStash<TraumaStatusComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref TraumaStatusComponent status =
                    ref _traumaStatusStash.Get(entity);

                if (status.Instances == null)
                    continue;

                for (int i = status.Instances.Count - 1; i >= 0; i--)
                {
                    TraumaInstance instance = status.Instances[i];
                    instance.RemainingDuration -= deltaTime;

                    if (instance.RemainingDuration <= 0f)
                    {
                        status.Instances.RemoveAt(i);
                        continue;
                    }

                    status.Instances[i] = instance;
                }
            }
        }

        public void Dispose() { }
    }
}
