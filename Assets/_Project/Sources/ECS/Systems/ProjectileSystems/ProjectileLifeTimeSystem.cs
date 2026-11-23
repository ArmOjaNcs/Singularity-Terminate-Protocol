using ECS.ProjectileComponents;
using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileLifeTimeSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<ProjectileComponent> _projectileStash;
        private ProjectileCleanup _cleanup;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ProjectileComponent>()
                .Build();

            _projectileStash = World.GetStash<ProjectileComponent>();
            _cleanup = new ProjectileCleanup(World);
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref ProjectileComponent projectile = ref _projectileStash.Get(entity);

                if (projectile.CleanupRequested)
                    continue;
                
                projectile.CurrentLifetime += deltaTime;

                if (projectile.Lifetime <= 0f ||
                    projectile.CurrentLifetime < projectile.Lifetime)
                    continue;
                
                _cleanup.Request(entity);
            }
        }

        public void Dispose() { }
    }
}