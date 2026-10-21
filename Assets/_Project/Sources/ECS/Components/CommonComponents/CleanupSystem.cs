using System.Collections.Generic;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
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
        private Stash<EnemyTag> _enemyTagStash;

        private readonly List<Entity> _requestsToRemove = new List<Entity>();
        private readonly HashSet<Entity> _entitiesToRemove = new HashSet<Entity>();

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
            _enemyTagStash = World.GetStash<EnemyTag>();
        }

        public void OnUpdate(float deltaTime)
        {
            _requestsToRemove.Clear();
            _entitiesToRemove.Clear();

            // Сначала определяем, какие сущности нужно удалить.
            // Во время обхода фильтров сущности не удаляем.
            foreach (Entity request in _filter)
            {
                ref CleanupRequest cleanupRequest =
                    ref _cleanupRequestStash.Get(request);

                Entity candidate = cleanupRequest.Candidate;

                if (candidate == default)
                {
                    _requestsToRemove.Add(request);
                    continue;
                }

                if (_enemyTagStash.Has(candidate))
                {
                    if (HasProjectiles(candidate))
                        continue;

                    CollectWeapons(candidate);
                    _entitiesToRemove.Add(candidate);
                }
                else if (_projectileStash.Has(candidate))
                {
                    _entitiesToRemove.Add(candidate);
                }

                _requestsToRemove.Add(request);
            }

            // Удаляем сущности только после завершения обхода фильтров.
            foreach (Entity entity in _entitiesToRemove)
                World.RemoveEntity(entity);

            foreach (Entity request in _requestsToRemove)
                World.RemoveEntity(request);
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

        private void CollectWeapons(Entity owner)
        {
            foreach (Entity weapon in _weaponFilter)
            {
                if (_weaponStash.Get(weapon).Owner != owner)
                    continue;

                _entitiesToRemove.Add(weapon);
            }
        }

        public void Dispose()
        {
            _requestsToRemove.Clear();
            _entitiesToRemove.Clear();
        }
    }
}