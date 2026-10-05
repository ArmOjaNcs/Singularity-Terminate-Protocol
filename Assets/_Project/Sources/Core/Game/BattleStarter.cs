using System;
using System.Collections.Generic;
using Cinemachine;
using Configs;
using ECS.CommonSystems;
using ECS.EnemySystems;
using ECS.PlayerSystems;
using ECS.ProjectileSystems;
using Gameplay;
using Gameplay.Items;
using Gameplay.Navigation;
using Gameplay.Player;
using Gameplay.Spatial;
using Gameplay.View;
using PlayerConfigs;
using Scellecs.Morpeh;
using UnityEngine;
using Zenject;

namespace Core.Game
{
    public sealed class BattleStarter
    {
        private PlayerConfig _playerConfig;
        private List<GemExpConfig> _itemConfigs;
        private Vector3 _playerSpawnPoint;
        private NavigationGrid _navigationGrid;
        private Transform _projectileContainer;
        private CinemachineVirtualCamera _camera;
        private PolygonCollider2D _cameraBounds;

        private World _world;
        private SystemsGroup _gameplaySystems;
        private SpatialGrid _spatialGrid;
        private PlayerFactory _playerFactory;
        private ItemFactory _itemFactory;
        private ViewPauseSystem _viewPauseSystem;
        private PlayerDeathSystem _playerDeathSystem;
        private GameObject _playerGameObject;

        private LevelBar _levelBar;

        [Inject]
        public void Construct(
            PlayerConfig playerConfig,
            List<GemExpConfig> itemConfigs,
            Vector3 spawnPoint,
            NavigationGrid navigationGrid,
            Transform projectileContainer,
            CinemachineVirtualCamera camera,
            PolygonCollider2D cameraBounds,
            LevelBar levelBar)
        {
            _playerConfig = playerConfig;
            _itemConfigs = itemConfigs;
            _playerSpawnPoint = spawnPoint;
            _navigationGrid = navigationGrid;
            _projectileContainer = projectileContainer;
            _camera = camera;
            _cameraBounds = cameraBounds;
            _levelBar = levelBar;

            Initialize();
        }

        private void Initialize()
        {
            CreateWorld();
            CreateSpatialGrid();
            CreateSystems();
            CreateFactories();
            CreatePlayer();
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

            _gameplaySystems.AddSystem(new PlayerExperienceSystem());
            _gameplaySystems.AddSystem(new LevelUpSystem());
            _gameplaySystems.AddSystem(new PlayerExperienceSystem());

            _gameplaySystems.AddSystem(new ItemScanSystem());
            _gameplaySystems.AddSystem(new ItemPickupSystem());

            _world.AddSystemsGroup(0, _gameplaySystems);
        }

        private void CreateFactories()
        {
            _playerFactory = new PlayerFactory(_world);
            _itemFactory = new ItemFactory(_world);
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