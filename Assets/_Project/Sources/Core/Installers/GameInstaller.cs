using Cinemachine;
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
        [SerializeField] private Vector3 _playerSpawnPoint;
        [SerializeField] private ArenaBounds _arenaBounds;
        [SerializeField] private NavigationGrid _navigationGrid;
        [SerializeField] private CinemachineVirtualCamera _camera;
        [SerializeField] private PolygonCollider2D _cameraBounds;

        public override void InstallBindings()
        {

        }
    }
}