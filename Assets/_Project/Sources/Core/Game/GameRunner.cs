using ECS.CommonComponents;
using ECS.CommonSystems;
using ECS.PlayerSystems;
using Gameplay;
using Gameplay.CameraScripts;
using Gameplay.Common;
using Gameplay.Navigation;
using Gameplay.Player;
using Gameplay.Spatial;
using PlayerConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Core.Game
{
    public sealed class GameRunner : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private Vector3 _playerSpawnPoint;
        [SerializeField] private CameraFollow _camera;
        [SerializeField] private ArenaBounds _arenaBounds;
        [SerializeField]
        private NavigationGrid _navigationGrid;

        private World _world;
        private SystemsGroup _gameplaySystems;

        private SpatialGrid _spatialGrid;

        private PlayerFactory _playerFactory;

        private void Awake()
        {
            CreateWorld();
            CreateSpatialGrid();
            CreateSystems();
            CreateFactories();
        }

        private void Start()
        {
            CreatePlayer();
        }

        private void CreateWorld()
        {
            _world = World.Default;
        }

        private void CreateSpatialGrid()
        {
            _spatialGrid = new SpatialGrid(2f);
        }

        private void CreateSystems()
        {
            _gameplaySystems =
            _world.CreateSystemsGroup();

            _gameplaySystems.AddSystem(
                new PlayerInputSystem());

            _gameplaySystems.AddSystem(
                new PlayerMovementSystem(
                    _navigationGrid));

            _gameplaySystems.AddSystem(
                new ViewPositionSystem());

            _gameplaySystems.AddSystem(
                new PlayerAnimationSystem());

            _world.AddSystemsGroup(
                0,
                _gameplaySystems);
        }

        private void CreateFactories()
        {
            _playerFactory = new PlayerFactory(_world);
        }

        private void CreatePlayer()
        {
            Vector3 spawnPosition = _playerSpawnPoint;

            Entity playerEntity = _playerFactory.Create(
                _playerConfig,
                spawnPosition);

            if (playerEntity == default)
                return;

            Stash<ViewComponent> viewStash =
                _world.GetStash<ViewComponent>();

            if (!viewStash.Has(playerEntity))
            {
                Debug.LogError(
                    "Created player does not contain ViewComponent.");

                return;
            }

            EntityView playerView = viewStash.Get(playerEntity).View;

            if (playerView == null)
            {
                Debug.LogError(
                    "Created player does not contain PlayerView.");

                return;
            }

            _camera.SetTarget(playerView.transform);
            _camera.SetBounds(_arenaBounds);
        }
    }
}