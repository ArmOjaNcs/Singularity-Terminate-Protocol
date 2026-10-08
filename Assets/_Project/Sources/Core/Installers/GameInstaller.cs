using System.Collections.Generic;
using Cinemachine;
using Configs;
using Core.Game;
using Gameplay;
using Gameplay.Navigation;
using PlayerConfigs;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private List<GemExpConfig> _itemConfigs;
        [SerializeField] private Vector3 _playerSpawnPoint;
        [SerializeField] private ArenaBounds _arenaBounds;
        [SerializeField] private NavigationGrid _navigationGrid;
        [SerializeField] private CinemachineVirtualCamera _camera;
        [SerializeField] private PolygonCollider2D _cameraBounds;
        [SerializeField] private Transform _projectileContainer;

        public override void InstallBindings()
        {
            BindParams();

            Container.Bind<BattleStarter>().AsSingle().NonLazy();
        }

        private void BindParams()
        {
            Container.BindInstance(_playerConfig).AsSingle();
            Container.BindInstance(_itemConfigs).AsSingle();
            Container.BindInstance(_playerSpawnPoint).AsSingle();
            Container.BindInstance(_arenaBounds).AsSingle();
            Container.BindInstance(_navigationGrid).AsSingle();
            Container.BindInstance(_camera).AsSingle();
            Container.BindInstance(_cameraBounds).AsSingle();
            Container.BindInstance(_projectileContainer).WithId("ProjectileContainer").AsSingle();
        }
    }
}