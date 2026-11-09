using System.Collections.Generic;
using System.Linq;
using Core.Services;
using ECS.WeaponSystems;
using EnemyConfigs;
using LootConfigs;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;
using UpgradeConfigs;
using Zenject;

namespace Core.Installers
{
    public class GlobalInstaller : MonoInstaller
    {
        [Header("Services")]
        [SerializeField] private AudioService _audioService;

        [Header("UpgradeDatabases")]
        [SerializeField] private List<NewWeaponUpgradeConfig> _weapons;
        [SerializeField] private List<WeaponStatUpgradeConfig> _weaponUpgrades;
        [SerializeField] private List<NewTurretUpgradeConfig> _turrets;
        [SerializeField] private List<TurretStatUpgradeConfig> _turretUpgrades;
        [SerializeField] private List<SkillUpgradeConfig> _skillUpgrades;

        [Header("Databases")]
        [SerializeField] private ProjectileDatabase _projectileDatabase;
        [SerializeField] private EnemyConfigsDatabase _enemyConfigsDatabase;
        [SerializeField] private EnemySpawnDatabase _enemySpawnDatabase;
        [SerializeField] private LootDropDatabase _lootDropDatabase;

        public override void InstallBindings()
        {
            BindInstances();
            BindServices();
        }

        private void BindInstances()
        {
            Container.BindInstance(World.Default).AsSingle();

            Container.BindInstance(_weapons).AsSingle();
            Container.BindInstance(_weaponUpgrades).AsSingle();
            Container.BindInstance(_turrets).AsSingle();
            Container.BindInstance(_turretUpgrades).AsSingle();
            Container.BindInstance(_skillUpgrades).AsSingle();
            Container.BindInstance(_lootDropDatabase).AsSingle();

            Container.BindInstance(_projectileDatabase).AsSingle();
            Container.BindInstance(_enemyConfigsDatabase).AsSingle();
            Container.BindInstance(_enemySpawnDatabase).AsSingle();
        }

        private void BindServices()
        {
            Container.BindInterfacesAndSelfTo<AudioService>()
                .FromComponentInNewPrefab(_audioService)
                .UnderTransformGroup("GlobalServices")
                .AsSingle();

            Container.Bind<UpgradeGenerator>().AsSingle();
            Container.Bind<WeaponFactory>().AsSingle();

            List<UpgradeConfig> upgradeConfigsList =
                _weapons
                .Concat<UpgradeConfig>(_turrets)
                .Concat<UpgradeConfig>(_skillUpgrades)
                .ToList();

            Container.Bind<UpgradeStorage>().AsSingle().WithArguments(upgradeConfigsList);
        }
    }
}
