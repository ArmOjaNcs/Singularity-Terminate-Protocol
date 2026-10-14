using System.Collections.Generic;
using Core.Services;
using UnityEngine;
using UpgradeConfigs;
using Zenject;

namespace Core.Installers
{
    public class GlobalInstaller : MonoInstaller
    {
        [SerializeField] private AudioService _audioService;
        [SerializeField] private List<WeaponUpgradeConfig> _weaponUpgrades;
        [SerializeField] private List<TurretUpgradeConfig> _turretUpgrades;
        [SerializeField] private List<SkillUpgradeConfig> _skillUpgrades;

        public override void InstallBindings()
        {
            BindInstances();
            BindServices();
        }

        private void BindInstances()
        {
            Container.BindInstance(_weaponUpgrades).AsSingle();
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
        }
    }
}
