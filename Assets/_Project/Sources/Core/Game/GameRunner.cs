using ECS.CommonSystems;
using ECS.EnemySystems;
using ECS.PlayerSystems;
using ECS.ProjectileSystems;
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
        [SerializeField] private NavigationGrid _navigationGrid;
        [SerializeField] private Transform _projectileContainer;

        private World _world;
        private SystemsGroup _gameplaySystems;
        private SpatialGrid _spatialGrid;
        private PlayerFactory _playerFactory;
        private ViewPauseSystem _viewPauseSystem;
        private PlayerDeathSystem _playerDeathSystem;
        private Entity _playerEntity;
        private bool _isPaused;

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                SetPause(!_isPaused);

            if (Input.GetKeyDown(KeyCode.R))
                _playerDeathSystem.Revive();
        }

        private void CreateWorld()
        {
            _world = World.Default;
        }

        private void CreateSpatialGrid()
        {
            _spatialGrid = new SpatialGrid(0.5f);
        }

        private void CreateSystems()
        {
            _gameplaySystems = _world.CreateSystemsGroup();

            _gameplaySystems.AddSystem(new PlayerInputSystem());
            _gameplaySystems.AddSystem(new PlayerMovementSystem(_navigationGrid));
            _gameplaySystems.AddSystem(new ViewPositionSystem());
            _gameplaySystems.AddSystem(new ViewSortingSystem());
            _gameplaySystems.AddSystem(new ProjectileSortingSystem());
            _viewPauseSystem = new ViewPauseSystem();
            _gameplaySystems.AddSystem(_viewPauseSystem);

            _gameplaySystems.AddSystem(new SpatialGridSystem(_spatialGrid));
            _gameplaySystems.AddSystem(new TargetSelectionSystem(_spatialGrid));

            _gameplaySystems.AddSystem(new EnemyAttackCalldownSystem());
            _gameplaySystems.AddSystem(new EnemyAttackAnimationSystem());
            _gameplaySystems.AddSystem(new ProjectileFactorySystem(_projectileContainer));
            _gameplaySystems.AddSystem(new ProjectileCollisionCheckSystem(_spatialGrid));
            _gameplaySystems.AddSystem(new ProjectileLifeTimeSystem());

            _gameplaySystems.AddSystem(new DamageSystem());

            _gameplaySystems.AddSystem(new PlayerAnimationSystem());
            _playerDeathSystem = new PlayerDeathSystem();
            _gameplaySystems.AddSystem(_playerDeathSystem);
            _gameplaySystems.AddSystem(new EnemyDeathAnimationSystem());
            _gameplaySystems.AddSystem(new DeathSystem());

            _world.AddSystemsGroup(0, _gameplaySystems);
        }

        private void CreateFactories()
        {
            _playerFactory = new PlayerFactory(_world);
        }

        private void CreatePlayer()
        {
            Vector3 spawnPosition = _playerSpawnPoint;

            _playerEntity =
                _playerFactory.Create(
                    _playerConfig,
                    spawnPosition);

            if (_playerEntity == default)
                return;
        }

        private void SetPause(bool value)
        {
            if (_isPaused == value)
                return;

            _isPaused = value;

            if (_isPaused)
            {
                _viewPauseSystem.Pause();
                _world.UpdateByUnity = false;

                return;
            }

            _world.UpdateByUnity = true;
            _viewPauseSystem.Play();
        }
    }
}