using ECS.CommonComponents;
using ECS.PlayerComponents;
using EnemyConfigs;
using Gameplay.Enemy;
using Gameplay.Navigation;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.EnemySystems
{
    public sealed class EnemySpawnSystem : ISystem
    {
        public World World { get; set; }

        private readonly EnemyFactory _enemyFactory;
        private readonly EnemyConfigsDatabase _enemyConfigsDatabase;
        private readonly EnemySpawnDatabase _enemySpawnDatabase;
        private readonly NavigationGrid _navigationGrid;

        private Filter _playerFilter;

        private Stash<PositionComponent> _positionStash;

        private readonly Dictionary<CreatureType, EnemyConfig> _enemyConfigs =
            new Dictionary<CreatureType, EnemyConfig>();

        private readonly List<WaveRuntime> _waves = new List<WaveRuntime>();
        private readonly List<Vector3> _candidatePositions = new List<Vector3>(4);

        private float _elapsedTime;

        public EnemySpawnSystem(
            EnemyFactory enemyFactory,
            EnemyConfigsDatabase enemyConfigsDatabase,
            EnemySpawnDatabase enemySpawnDatabase,
            NavigationGrid navigationGrid)
        {
            _enemyFactory = enemyFactory;
            _enemyConfigsDatabase = enemyConfigsDatabase;
            _enemySpawnDatabase = enemySpawnDatabase;
            _navigationGrid = navigationGrid;
        }

        public void OnAwake()
        {
            _playerFilter = World.Filter
                .With<PlayerTag>()
                .With<PositionComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();

            InitializeEnemyConfigs();
            InitializeWaves();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_enemyFactory == null ||
                _enemySpawnDatabase == null ||
                _navigationGrid == null)
            {
                return;
            }

            if (!TryGetPlayerPosition(out Vector3 playerPosition))
                return;

            _elapsedTime += deltaTime;

            foreach (WaveRuntime wave in _waves)
            {
                if (_elapsedTime < wave.Config.StartTime)
                    continue;

                SpawnWave(wave, playerPosition, deltaTime);
            }
        }

        public void Dispose()
        {
            _waves.Clear();
            _enemyConfigs.Clear();
            _candidatePositions.Clear();
        }

        private void InitializeEnemyConfigs()
        {
            if (_enemyConfigsDatabase == null ||
                _enemyConfigsDatabase.Enemies == null)
            {
                return;
            }

            foreach (EnemyConfig config in _enemyConfigsDatabase.Enemies)
            {
                if (config == null)
                    continue;

                if (_enemyConfigs.ContainsKey(config.CreatureType))
                {
                    UnityEngine.Debug.LogWarning(
                        $"EnemySpawnSystem: duplicate config for " +
                        $"{config.CreatureType}. The first config will be used.",
                        config);

                    continue;
                }

                _enemyConfigs.Add(config.CreatureType, config);
            }
        }

        private void InitializeWaves()
        {
            if (_enemySpawnDatabase == null ||
                _enemySpawnDatabase.Waves == null)
            {
                return;
            }

            foreach (EnemyWaveSpawnConfig waveConfig in _enemySpawnDatabase.Waves)
            {
                if (waveConfig == null || waveConfig.Enemies == null)
                    continue;

                WaveRuntime wave = new WaveRuntime(waveConfig);

                foreach (EnemySpawnConfig spawnConfig in waveConfig.Enemies)
                {
                    if (spawnConfig == null || spawnConfig.Count <= 0)
                        continue;

                    wave.Enemies.Add(new EnemySpawnRuntime(spawnConfig));
                }

                _waves.Add(wave);
            }

            _waves.Sort((first, second) =>
                first.Config.StartTime.CompareTo(second.Config.StartTime));
        }

        private void SpawnWave(
            WaveRuntime wave,
            Vector3 playerPosition,
            float deltaTime)
        {
            foreach (EnemySpawnRuntime enemy in wave.Enemies)
            {
                if (enemy.SpawnedCount >= enemy.Config.Count)
                    continue;

                enemy.Timer -= deltaTime;

                if (enemy.Timer > 0f)
                    continue;

                if (!_enemyConfigs.TryGetValue(
                        enemy.Config.EnemyType,
                        out EnemyConfig enemyConfig))
                {
                    UnityEngine.Debug.LogWarning(
                        $"EnemySpawnSystem: config for " +
                        $"{enemy.Config.EnemyType} was not found.");

                    enemy.Timer = _enemySpawnDatabase.RetryInterval;
                    continue;
                }

                if (enemyConfig.Collider == null)
                {
                    enemy.Timer = _enemySpawnDatabase.RetryInterval;
                    continue;
                }

                if (!TryFindSpawnPosition(
                        playerPosition,
                        enemyConfig,
                        out Vector3 spawnPosition))
                {
                    enemy.Timer = Mathf.Max(
                        0.1f,
                        _enemySpawnDatabase.RetryInterval);

                    continue;
                }

                Entity entity = _enemyFactory.Create(
                    enemyConfig,
                    spawnPosition);

                if (entity == default)
                {
                    enemy.Timer = Mathf.Max(
                        0.1f,
                        _enemySpawnDatabase.RetryInterval);

                    continue;
                }

                enemy.SpawnedCount++;

                enemy.Timer = Mathf.Max(0f, enemy.Config.SpawnTime);
            }
        }

        private bool TryGetPlayerPosition(out Vector3 position)
        {
            foreach (Entity player in _playerFilter)
            {
                position = _positionStash.Get(player).Position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        private bool TryFindSpawnPosition(
            Vector3 playerPosition,
            EnemyConfig enemyConfig,
            out Vector3 spawnPosition)
        {
            ColliderComponent collider = CreateCollider(enemyConfig);

            _candidatePositions.Clear();

            // Сначала проверяем четыре стороны света.
            AddCardinalCandidates(playerPosition);

            for (int i = 0; i < _candidatePositions.Count; i++)
            {
                int randomIndex = Random.Range(i, _candidatePositions.Count);

                Vector3 temp = _candidatePositions[i];
                _candidatePositions[i] = _candidatePositions[randomIndex];
                _candidatePositions[randomIndex] = temp;
            }

            foreach (Vector3 candidate in _candidatePositions)
            {
                if (!_navigationGrid.IsWalkable(candidate, collider))
                    continue;

                spawnPosition = candidate;
                return true;
            }

            // Если ни одна сторона не подходит, пробуем случайные направления.
            int attempts = Mathf.Max(1, _enemySpawnDatabase.PositionAttempts);

            for (int i = 0; i < attempts; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

                float distance = Random.Range(
                    _enemySpawnDatabase.MinSpawnDistance,
                    Mathf.Max(
                        _enemySpawnDatabase.MinSpawnDistance,
                        _enemySpawnDatabase.MaxSpawnDistance));

                Vector3 direction = new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    0f);

                Vector3 candidate = playerPosition + direction * distance;

                if (!_navigationGrid.IsWalkable(candidate, collider))
                    continue;

                spawnPosition = candidate;
                return true;
            }

            spawnPosition = Vector3.zero;
            return false;
        }

        private void AddCardinalCandidates(Vector3 playerPosition)
        {
            float minDistance = _enemySpawnDatabase.MinSpawnDistance;
            float maxDistance = Mathf.Max(
                minDistance,
                _enemySpawnDatabase.MaxSpawnDistance);

            Vector3[] directions =
            {
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.right
            };

            foreach (Vector3 direction in directions)
            {
                float distance = Random.Range(minDistance, maxDistance);
                Vector3 candidate = playerPosition + direction * distance;

                // Отбрасываем точки за пределами сетки и на заблокированных клетках.
                if (!_navigationGrid.IsWalkable(candidate))
                    continue;

                _candidatePositions.Add(candidate);
            }
        }

        private ColliderComponent CreateCollider(EnemyConfig config)
        {
            return new ColliderComponent
            {
                Type = config.Collider.Type,
                Radius = config.Collider.Radius,
                Length = config.Collider.Length,
                Size = config.Collider.Size,
                Angle = 0f,
                CapsuleOrientation = config.Collider.CapsuleOrientation
            };
        }

        private sealed class WaveRuntime
        {
            public readonly EnemyWaveSpawnConfig Config;
            public readonly List<EnemySpawnRuntime> Enemies =
                new List<EnemySpawnRuntime>();

            public WaveRuntime(EnemyWaveSpawnConfig config)
            {
                Config = config;
            }
        }
    }
}