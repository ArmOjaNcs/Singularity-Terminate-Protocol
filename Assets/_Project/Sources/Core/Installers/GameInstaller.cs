using Cinemachine;
using Core.Game;
using ECS.CommonComponents;
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
        [Header("Timer")]
        [SerializeField] private float _maxBattleTime = 30000f;

        [Header("Player")]
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private Vector3 _playerSpawnPoint;

        [Header("Arena")]
        [SerializeField] private NavigationGrid _navigationGrid;

        [Header("Camera")]
        [SerializeField] private CinemachineVirtualCamera _camera;
        [SerializeField] private PolygonCollider2D _cameraBounds;

        [Header("Containers")]
        [SerializeField] private Transform _projectileContainer;
        [SerializeField] private Transform _enemyContainer;
        [SerializeField] private Transform _lootContainer;
        [SerializeField] private Transform _damageableContainer;

        public override void InstallBindings()
        {
            BindInstances();

            Container.Bind<float>().WithId("MaxBattleTime").FromInstance(_maxBattleTime);
            Container.Bind<BattleStatistics>().AsSingle().NonLazy();
            Container.Bind<BattleStarter>().AsSingle().NonLazy();
        }

        private void BindInstances()
        {
            BindPlayerInstances();
            BindArenaInstances();
            BindCameraInstances();
            BindContainerInstances();
        }

        private void BindPlayerInstances()
        {
            Container.BindInstance(_playerConfig).AsSingle();
            Container.BindInstance(_playerSpawnPoint).AsSingle();
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

            Container.BindInstance(_lootContainer)
                .WithId("LootContainer");

            Container.BindInstance(_damageableContainer)
                .WithId("DamageableContainer");
        }
    }
}