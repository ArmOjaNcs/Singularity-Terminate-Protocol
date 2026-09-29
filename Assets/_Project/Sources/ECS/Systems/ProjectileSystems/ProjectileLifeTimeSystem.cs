using ECS.ProjectileComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileLifeTimeSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<ProjectileComponent> _projectileStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .Build();

            _projectileStash =
                World.GetStash<ProjectileComponent>();

            _projectileViewStash =
                World.GetStash<ProjectileViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(entity);

                projectile.CurrentLifetime += deltaTime;

                if (projectile.Lifetime <= 0f ||
                    projectile.CurrentLifetime < projectile.Lifetime)
                    continue;

                DisableView(entity);

                World.RemoveEntity(entity);
            }
        }

        private void DisableView(Entity entity)
        {
            if (!_projectileViewStash.Has(entity))
                return;

            AnimatedView view = _projectileViewStash.Get(entity).View;

            if (view != null)
                view.SetActive(false);
        }

        public void Dispose()
        {
        }
    }
}