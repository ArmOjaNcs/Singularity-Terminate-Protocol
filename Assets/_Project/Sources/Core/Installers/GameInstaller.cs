using Cinemachine;
using Core.Game;
using EnemyConfigs;
using Gameplay.Navigation;
using PlayerConfigs;
using ProjectileConfigs;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Player")]
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private Vector3 _playerSpawnPoint;

        [Header("Databases")]
        [SerializeField] private ProjectileDatabase _projectileDatabase;
        [SerializeField] private EnemyConfigsDatabase _enemyConfigsDatabase;
        [SerializeField] private EnemySpawnDatabase _enemySpawnDatabase;

        [Header("Arena")]
        [SerializeField] private NavigationGrid _navigationGrid;

        [Header("Camera")]
        [SerializeField] private CinemachineVirtualCamera _camera;
        [SerializeField] private PolygonCollider2D _cameraBounds;

        [Header("Containers")]
        [SerializeField] private Transform _projectileContainer;
        [SerializeField] private Transform _enemyContainer;

        public override void InstallBindings()
        {
            BindInstances();

            Container.Bind<BattleStarter>().AsSingle().NonLazy();
        }

        private void BindInstances()
        {
            BindPlayerInstances();
            BindDatabases();
            BindArenaInstances();
            BindCameraInstances();
            BindContainerInstances();
        }

        private void BindPlayerInstances()
        {
            Container.BindInstance(_playerConfig).AsSingle();
            Container.BindInstance(_playerSpawnPoint).AsSingle();
        }

        private void BindDatabases()
        {
            Container.BindInstance(_projectileDatabase).AsSingle();
            Container.BindInstance(_enemyConfigsDatabase).AsSingle();
            Container.BindInstance(_enemySpawnDatabase).AsSingle();
        }

        private void BindArenaInstances()
        {
            Container.BindInstance(_navigationGrid).AsSingle();
        }

        private void BindCameraInstances()
        {
            Container.BindInstance(_camera).AsSingle();
            Container.BindInstance(_cameraBounds).AsSingle();
        }

        private void BindContainerInstances()
        {
            Container.BindInstance(_projectileContainer)
                .WithId("ProjectileContainer");

            Container.BindInstance(_enemyContainer)
                .WithId("EnemyContainer");
        }
    }
}