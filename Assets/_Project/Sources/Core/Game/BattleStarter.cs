using System;
using System.Collections.Generic;
using Cinemachine;
using Configs;
using Core.Services;
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
using Gameplay.Items;
using Gameplay.Navigation;
using Gameplay.Player;
using Gameplay.Spatial;
using Gameplay.View;
using PlayerConfigs;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;
using Zenject;

namespace Core.Game
{
    public sealed class BattleStarter
    {
        private PlayerConfig _playerConfig;
        private ProjectileDatabase _projectileDatabase;
        private List<GemExpConfig> _itemConfigs;
        private Vector3 _playerSpawnPoint;
        private NavigationGrid _navigationGrid;
        private Transform _projectileContainer;
        private CinemachineVirtualCamera _camera;
        private PolygonCollider2D _cameraBounds;
        private EnemyConfig _enemyConfig;
        private Vector3 _enemySpawnPoint;

        private World _world;
        private SystemsGroup _gameplaySystems;
        private SpatialGrid _spatialGrid;

        private WeaponFactory _weaponFactory;
        private PlayerFactory _playerFactory;
        private EnemyFactory _enemyFactory;
        private ProjectileFactory _projectileFactory;
        private ItemFactory _itemFactory;

        private ViewPauseSystem _viewPauseSystem;
        private PlayerDeathSystem _playerDeathSystem;
        private GameObject _playerGameObject;
        private DamageStatistics _damageStatistics;

        private LevelBar _levelBar;
        private UIService _uiService;

        [Inject]
        public void Construct(
            PlayerConfig playerConfig,
            ProjectileDatabase projectileDatabase,
            List<GemExpConfig> itemConfigs,
            Vector3 spawnPoint,
            NavigationGrid navigationGrid,
            [Inject(Id = "ProjectileContainer")] Transform projectileContainer,
            CinemachineVirtualCamera camera,
            PolygonCollider2D cameraBounds,
            LevelBar levelBar,
            EnemyConfig enemyConfig,
            Vector3 enemySpawnPoint,
            UIService uiService)
        {
            _playerConfig = playerConfig;
            _projectileDatabase = projectileDatabase;
            _itemConfigs = itemConfigs;
            _playerSpawnPoint = spawnPoint;
            _navigationGrid = navigationGrid;
            _projectileContainer = projectileContainer;
            _camera = camera;
            _cameraBounds = cameraBounds;
            _levelBar = levelBar;
            _enemySpawnPoint = enemySpawnPoint;
            _enemyConfig = enemyConfig;
            _uiService = uiService;

            Initialize();
        }

        private void Initialize()
        {
            CreateWorld();
            CreateSpatialGrid();
            CreateFactories();
            CreateSystems();
            CreatePlayer();
            CreateEnemy();
            SetUI();
            CreateItems();
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
            _itemFactory = new ItemFactory(_world);

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

            _gameplaySystems.AddSystem(new ItemScanSystem());
            _gameplaySystems.AddSystem(new ItemPickupSystem());

            _gameplaySystems.AddSystem(new PlayerExperienceSystem());
            _gameplaySystems.AddSystem(new LevelUpSystem(_uiService));
        }

        private void AddStatisticSystems()
        {
            _damageStatistics = new DamageStatistics();
            _gameplaySystems.AddSystem(new DamageStatisticsSystem(_damageStatistics));
        }

        private void CreatePlayer()
        {
            Vector3 spawnPosition = _playerSpawnPoint;

            _playerGameObject =
                _playerFactory.Create(
                    _playerConfig,
                    spawnPosition);

            SetCamera(_playerGameObject.transform);
        }

        private void CreateEnemy()
        {
            _enemyFactory.Create(_enemyConfig, _enemySpawnPoint);
        }

        private void SetUI()
        {
            Entity playerEntity = _playerGameObject.GetComponent<AnimatedCreature>().Entity;
            var xpStash = _world.GetStash<PlayerExperienceComponent>();

            if (xpStash.Has(playerEntity))
            {
                PlayerExperienceComponent xpComponent = xpStash.Get(playerEntity);
                _levelBar.Setup(xpComponent);
            }
        }

        private void CreateItems()
        {
            _itemFactory.Create(_itemConfigs, _playerSpawnPoint + new Vector3(5, 5));
            _itemFactory.Create(_itemConfigs, _playerSpawnPoint + new Vector3(-5, 5));
            _itemFactory.Create(_itemConfigs, _playerSpawnPoint + new Vector3(5, -5));
            _itemFactory.Create(_itemConfigs, _playerSpawnPoint + new Vector3(10, 10));
            _itemFactory.Create(_itemConfigs, _playerSpawnPoint + new Vector3(-10, 10));
            _itemFactory.Create(_itemConfigs, _playerSpawnPoint + new Vector3(10, -10));
        }

        private void SetCamera(Transform playerTransform)
        {
            if (_camera == null)
                throw new ArgumentNullException(nameof(_camera));

            _camera.LookAt = playerTransform;
            _camera.Follow = playerTransform;

            CinemachineConfiner2D confiner = _camera.GetComponent<CinemachineConfiner2D>();

            if (confiner != null && _cameraBounds != null)
            {
                confiner.m_BoundingShape2D = _cameraBounds.GetComponent<Collider2D>();
                confiner.InvalidateCache();
            }
        }
    }
}