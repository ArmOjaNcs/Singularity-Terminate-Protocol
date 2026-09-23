using ECS.PlayerSystems;
using Gameplay.Player;
using PlayerConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Core.Game
{
    public sealed class GameRunner : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private Vector3 _playerSpawnPoint;

        private World _world;
        private SystemsGroup _gameplaySystems;

        private PlayerFactory _playerFactory;

        private void Awake()
        {
            CreateWorld();
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

        private void CreateSystems()
        {
            _gameplaySystems = _world.CreateSystemsGroup();

            _gameplaySystems.AddSystem(new PlayerInputSystem());
            _gameplaySystems.AddSystem(new PlayerMovementSystem());

            _world.AddSystemsGroup(0, _gameplaySystems);
        }

        private void CreateFactories()
        {
            _playerFactory = new PlayerFactory(_world);
        }

        private void CreatePlayer()
        {
            Vector3 spawnPosition = _playerSpawnPoint != null
                ? _playerSpawnPoint
                : Vector3.zero;

            _playerFactory.Create(
                _playerConfig,
                spawnPosition
            );
        }
    }
}