using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    internal sealed class ProjectileCleanup
    {
        private readonly World _world;
        private readonly Stash<ProjectileComponent> _projectiles;
        private readonly Stash<ProjectileViewComponent> _views;
        private readonly Stash<CleanupRequest> _requests;

        public ProjectileCleanup(World world)
        {
            _world = world;
            _projectiles = world.GetStash<ProjectileComponent>();
            _views = world.GetStash<ProjectileViewComponent>();
            _requests = world.GetStash<CleanupRequest>();
        }

        public void Request(Entity entity)
        {
            ref ProjectileComponent projectile = ref _projectiles.Get(entity);
            if (projectile.CleanupRequested)
                return;

            projectile.CleanupRequested = true;
            if (_views.Has(entity))
            {
                AnimatedView view = _views.Get(entity).View;
                if (view != null)
                    view.SetActive(false);
            }

            _requests.Set(_world.CreateEntity(), new CleanupRequest { Candidate = entity });
        }
    }
}
