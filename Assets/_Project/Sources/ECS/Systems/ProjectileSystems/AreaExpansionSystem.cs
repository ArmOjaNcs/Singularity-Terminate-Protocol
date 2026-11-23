using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class AreaExpansionSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Stash<AreaExpansionComponent> _areaStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<ProjectileViewComponent> _viewStash;
        private ProjectileCleanup _cleanup;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<AreaExpansionComponent>()
                .With<HitBoxComponent>()
                .With<ProjectileComponent>()
                .Build();

            _areaStash = World.GetStash<AreaExpansionComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _viewStash = World.GetStash<ProjectileViewComponent>();
            _cleanup = new ProjectileCleanup(World);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref AreaExpansionComponent area = ref _areaStash.Get(entity);
                if (area.ReachedMaximum)
                {
                    _cleanup.Request(entity);
                    continue;
                }

                area.Elapsed += deltaTime;
                float progress = area.Duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(area.Elapsed / area.Duration);

                _hitBoxStash.Get(entity).Collider =
                    ColliderScaler.Scale(area.BaseCollider, progress);

                if (_viewStash.Has(entity) &&
                    _viewStash.Get(entity).View is IAreaExpansionView view)
                    view.SetExpansionProgress(progress);

                if (progress >= 1f)
                    area.ReachedMaximum = true;
            }
        }

        public void Dispose() { }
    }
}
