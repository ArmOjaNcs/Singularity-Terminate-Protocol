using ECS.CommonSystems;
using ECS.ProjectileSystems;
using ECS.TrajectorySystems;
using Gameplay.Navigation;
using Gameplay.Spatial;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.WeaponSystems
{
    internal sealed class WeaponCombatSystems
    {
        private readonly SpatialGrid _grid;
        private readonly ProjectileDatabase _database;
        private readonly Transform _container;
        private readonly NavigationGrid _navigation;
        private readonly SecondaryProjectileSpawner _secondaryProjectiles;

        public ProjectileReleaseScheduler Releases { get; }

        public WeaponCombatSystems(World world, ProjectileDatabase database, Transform container, SpatialGrid grid,
            NavigationGrid navigation)
        {
            _grid = grid;
            _database = database;
            _container = container;
            _navigation = navigation;
            var factory = new ProjectileFactory(world, database, container, grid);
            Releases = new ProjectileReleaseScheduler(world, factory);
            _secondaryProjectiles = new SecondaryProjectileSpawner(world, factory);
        }

        public void AddAttacks(SystemsGroup systems)
        {
            systems.AddSystem(new TurretDeploymentSystem(_container, _navigation));
            systems.AddSystem(new AttackCooldownSystem());
            systems.AddSystem(new AttackSystem());
            systems.AddSystem(new AttackExecutionSystem(Releases));
        }

        public void AddProjectileMotion(SystemsGroup systems)
        {
            systems.AddSystem(new ProjectileCreationSystem(Releases));
            systems.AddSystem(new StraightTrajectorySystem());
            systems.AddSystem(new ShrapnelFragmentFlightSystem());
            systems.AddSystem(new ElectricTrajectorySystem(_grid));
            systems.AddSystem(new ArcTrajectorySystem());
            systems.AddSystem(new SwordThrustSystem());
            systems.AddSystem(new GrenadeLauncherFlightSystem(_secondaryProjectiles));
            systems.AddSystem(new RocketLauncherFlightSystem());
            systems.AddSystem(new TacticalRocketFlightSystem());
            systems.AddSystem(new EmpWaveSystem(_database, _container));
            systems.AddSystem(new AreaExpansionSystem());
            systems.AddSystem(new RipperTrajectorySystem());
            systems.AddSystem(new ProjectileViewSystem());
        }

        public void AddProjectileHits(SystemsGroup systems)
        {
            systems.AddSystem(new ProjectileCollisionCheckSystem(_grid, _secondaryProjectiles));
            systems.AddSystem(new ProjectileLifeTimeSystem());
        }

        // DamageSystem produces the effect requests consumed in this phase.
        public void AddHitEffects(SystemsGroup systems)
        {
            systems.AddSystem(new EffectSystem());
            systems.AddSystem(new RicochetSystem(_grid));
            systems.AddSystem(new AttackSequenceSystem());
        }
    }
}
