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
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<ProjectileViewComponent> _viewStash;
        private Stash<CleanupRequest> _cleanupStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<AreaExpansionComponent>()
                .With<HitBoxComponent>()
                .With<ProjectileComponent>()
                .Build();

            _areaStash = World.GetStash<AreaExpansionComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _viewStash = World.GetStash<ProjectileViewComponent>();
            _cleanupStash = World.GetStash<CleanupRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref AreaExpansionComponent area = ref _areaStash.Get(entity);
                if (area.ReachedMaximum)
                {
                    RequestCleanup(entity);
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

        private void RequestCleanup(Entity entity)
        {
            ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
            if (projectile.CleanupRequested)
                return;

            projectile.CleanupRequested = true;
            if (_viewStash.Has(entity))
            {
                AnimatedView view = _viewStash.Get(entity).View;
                if (view != null)
                    view.SetActive(false);
            }

            Entity request = World.CreateEntity();
            _cleanupStash.Set(request, new CleanupRequest { Candidate = entity });
        }

        public void Dispose() { }
    }
}
