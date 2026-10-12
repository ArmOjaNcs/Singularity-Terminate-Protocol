using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileFactory
    {
        private readonly Stash<WeaponComponent> _weaponStash;

        private readonly IProjectileCreator _meleeCreator;
        private readonly IProjectileCreator _axeCreator;

        public ProjectileFactory(World world, ProjectileDatabase database, Transform container)
        {
            _weaponStash = world.GetStash<WeaponComponent>();

            ProjectileConfig meleeConfig = database.Get(ProjectileType.Melee);
            _meleeCreator = new MeleeProjectileCreator(world, meleeConfig);

            ProjectileConfig axeConfig = database.Get(ProjectileType.Axe);
            _axeCreator = new AxeProjectileCreator(world, axeConfig, container);
        }

        public void Begin(Entity weapon)
        {
            GetCreator(weapon)?.Begin(weapon);
        }

        public void Update(Entity weapon, float deltaTime)
        {
            GetCreator(weapon)?.Update(weapon, deltaTime);
        }

        public void Cancel(Entity weapon)
        {
            GetCreator(weapon)?.Cancel(weapon);
        }

        private IProjectileCreator GetCreator(Entity weapon)
        {
            if (!_weaponStash.Has(weapon))
                return null;

            WeaponType type = _weaponStash.Get(weapon).Type;

            return type switch
            {
                WeaponType.MeleeDefault => _meleeCreator,
                WeaponType.Axe => _axeCreator,
                _ => null
            };
        }
    }
}