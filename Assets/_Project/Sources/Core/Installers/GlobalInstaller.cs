using System.Collections.Generic;
using System.Linq;
using Core.Services;
using ECS.WeaponSystems;
using Scellecs.Morpeh;
using UnityEngine;
using UpgradeConfigs;
using Zenject;

namespace Core.Installers
{
    public class GlobalInstaller : MonoInstaller
    {
        [SerializeField] private AudioService _audioService;
        [SerializeField] private List<NewWeaponUpgradeConfig> _weapons;
        [SerializeField] private List<WeaponUpgradeConfig> _weaponUpgrades;
        [SerializeField] private List<NewTurretUpgradeConfig> _turrets;
        [SerializeField] private List<TurretUpgradeConfig> _turretUpgrades;
        [SerializeField] private List<SkillUpgradeConfig> _skillUpgrades;

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
        }

        private void BindServices()
        {
            Container.BindInterfacesAndSelfTo<AudioService>()
                .FromComponentInNewPrefab(_audioService)
                .UnderTransformGroup("GlobalServices")
                .AsSingle();

            Container.Bind<UpgradeGenerator>().AsSingle();
            Container.Bind<WeaponFactory>().AsSingle();

            List<UpgradeConfig> upgradeConfigsList = _weapons
                .Concat<UpgradeConfig>(_weaponUpgrades)
                .Concat<UpgradeConfig>(_turrets)
                .Concat<UpgradeConfig>(_turretUpgrades)
                .Concat<UpgradeConfig>(_skillUpgrades)
                .ToList();

            Container.Bind<UpgradeStorage>().AsSingle().WithArguments(upgradeConfigsList);
        }
    }
}
