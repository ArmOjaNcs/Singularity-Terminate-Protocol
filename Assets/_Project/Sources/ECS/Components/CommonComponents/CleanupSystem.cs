using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
using ECS.ViewComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;

namespace ECS.CommonSystems
{
    public sealed class CleanupSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Filter _projectileFilter;
        private Filter _weaponFilter;

        private Stash<CleanupRequest> _cleanupRequestStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<ViewComponent> _viewStash;
        private Stash<EnemyTag> _enemyTagStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<CleanupRequest>()
                .Build();

            _projectileFilter = World.Filter
                .With<ProjectileComponent>()
                .Build();

            _weaponFilter = World.Filter
                .With<WeaponComponent>()
                .Build();

            _cleanupRequestStash = World.GetStash<CleanupRequest>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _viewStash = World.GetStash<ViewComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity request in _filter)
            {
                ref CleanupRequest cleanupRequest =
                    ref _cleanupRequestStash.Get(request);

                Entity candidate = cleanupRequest.Candidate;

                if (candidate == default)
                {
                    World.RemoveEntity(request);
                    continue;
                }

                if (_enemyTagStash.Has(candidate))
                {
                    if (HasProjectiles(candidate))
                    {
                        continue;
                    }

                    CleanupEnemy(candidate);
                }
                else if (_projectileStash.Has(candidate))
                {
                    CleanupProjectile(candidate);
                }
                else
                {
                    World.RemoveEntity(request);
                    continue;
                }

                World.RemoveEntity(request);
            }
        }

        private bool HasProjectiles(Entity owner)
        {
            foreach (Entity projectile in _projectileFilter)
            {
                Entity weapon = _projectileStash.Get(projectile).Weapon;

                if (!_weaponStash.Has(weapon))
                    continue;

                if (_weaponStash.Get(weapon).Owner == owner)
                    return true;
            }

            return false;
        }

        private void CleanupEnemy(Entity enemy)
        {
            if (HasProjectiles(enemy))
                return;

            CleanupWeapons(enemy);
            World.RemoveEntity(enemy);
        }

        private void CleanupWeapons(Entity owner)
        {
            foreach (Entity weapon in _weaponFilter)
            {
                if (_weaponStash.Get(weapon).Owner != owner)
                    continue;

                World.RemoveEntity(weapon);
            }
        }

        private void CleanupProjectile(Entity projectile)
        {
            World.RemoveEntity(projectile);
        }

        public void Dispose() { }
    }
}