using ECS.CommonSystems;
using ECS.DamageComponents;
using ECS.DamageSystems;
using ECS.EnemySystems;
using ECS.PlayerSystems;
using ECS.ProjectileSystems;
using ECS.TrajectorySystems;
using ECS.WeaponSystems;
using EnemyConfigs;
using Gameplay.Enemy;
using Gameplay.Navigation;
using Gameplay.Player;
using Gameplay.Spatial;
using PlayerConfigs;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Core.Game
{
    public sealed class GameRunner : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private ProjectileDatabase _projectileDatabase;
        [SerializeField] private Vector3 _playerSpawnPoint;
        [SerializeField] private NavigationGrid _navigationGrid;
        [SerializeField] private Transform _projectileContainer;
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private Vector3 _enemySpawnPoint;

        private World _world;
        private SystemsGroup _gameplaySystems;

        private SpatialGrid _spatialGrid;

        private WeaponFactory _weaponFactory;
        private PlayerFactory _playerFactory;
        private EnemyFactory _enemyFactory;
        private ProjectileFactory _projectileFactory;

        private ViewPauseSystem _viewPauseSystem;
        private PlayerDeathSystem _playerDeathSystem;

        private GameObject _playerGameObject;
        private DamageStatistics _damageStatistics;
        private bool _isPaused;

        private void Awake()
        {
            CreateWorld();
            CreateSpatialGrid();
            CreateFactories();
            CreateSystems();
        }

        private void Start()
        {
            CreatePlayer();
            CreateEnemy();
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

        private void CreateFactories()
        {
            _weaponFactory = new WeaponFactory(_world);
            _playerFactory = new PlayerFactory(_world, _weaponFactory);
            _enemyFactory = new EnemyFactory(_world, _weaponFactory);

            _projectileFactory = new ProjectileFactory(
                _world,
                _projectileDatabase,
                _projectileContainer);
        }

        private void CreateSystems()
        {
            _gameplaySystems = _world.CreateSystemsGroup();

            AddMovementSystems();
            AddTargetingSystems();
            AddAttackSystems();
            AddProjectileSystems();
            AddCombatSystems();
            AddViewSystems();
            AddLifecycleSystems();
            AddStatisticSystems();

            _world.AddSystemsGroup(0, _gameplaySystems);
        }

        private void AddMovementSystems()
        {
            _gameplaySystems.AddSystem(new PlayerInputSystem());
            _gameplaySystems.AddSystem(new PlayerMovementSystem(_navigationGrid));
            _gameplaySystems.AddSystem(new WeaponPositionSystem());
        }

        private void AddTargetingSystems()
        {
            _gameplaySystems.AddSystem(new SpatialGridSystem(_spatialGrid));
            _gameplaySystems.AddSystem(new TargetSelectionSystem(_spatialGrid));
        }

        private void AddAttackSystems()
        {
            _gameplaySystems.AddSystem(new AttackCooldownSystem());
            _gameplaySystems.AddSystem(new AttackSystem());
            _gameplaySystems.AddSystem(new AttackExecutionSystem(_projectileFactory));

            _gameplaySystems.AddSystem(new EnemyAttackAnimationSystem());
        }

        private void AddProjectileSystems()
        {
            _gameplaySystems.AddSystem(new ProjectileCreationSystem(_projectileFactory));
            _gameplaySystems.AddSystem(new ArcTrajectorySystem());
            _gameplaySystems.AddSystem(new ProjectileViewSystem());
            _gameplaySystems.AddSystem(new ProjectileCollisionCheckSystem(_spatialGrid));
            _gameplaySystems.AddSystem(new ProjectileLifeTimeSystem());
        }

        private void AddCombatSystems()
        {
            _gameplaySystems.AddSystem(new DamageSystem());
            _gameplaySystems.AddSystem(new EffectSystem());
            _gameplaySystems.AddSystem(new KnockbackSystem());
        }

        private void AddViewSystems()
        {
            _gameplaySystems.AddSystem(new ViewPositionSystem());
            _gameplaySystems.AddSystem(new ViewSortingSystem());
            _gameplaySystems.AddSystem(new ProjectileSortingSystem());

            _viewPauseSystem = new ViewPauseSystem();
            _gameplaySystems.AddSystem(_viewPauseSystem);

            _gameplaySystems.AddSystem(new PlayerAnimationSystem());
        }

        private void AddLifecycleSystems()
        {
            _playerDeathSystem = new PlayerDeathSystem();
            _gameplaySystems.AddSystem(new CleanupSystem());
            _gameplaySystems.AddSystem(_playerDeathSystem);
            _gameplaySystems.AddSystem(new EnemyDeathSystem());
        }

        private void AddStatisticSystems()
        {
            _damageStatistics = new DamageStatistics();
            _gameplaySystems.AddSystem(new DamageStatisticsSystem(_damageStatistics));
        }

        private void CreatePlayer()
        {
            Vector3 spawnPosition = _playerSpawnPoint;

            _playerGameObject = _playerFactory.Create(
                _playerConfig,
                spawnPosition);
        }

        private void CreateEnemy()
        {
            _enemyFactory.Create(_enemyConfig, _enemySpawnPoint);
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