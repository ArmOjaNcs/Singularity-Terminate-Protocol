using System;
using System.Collections.Generic;
using Configs;
using DamageableConfigs;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using EnemyConfigs;
using Gameplay.Items;
using Gameplay.Navigation;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.DamageableItemSystems
{
    public sealed class DamageableItemsSpawnSystem : ISystem
    {
        private const float CardinalLateralSpread = 0.15f;

        private readonly DamageableItemsFactory _damageableItemsFactory;
        private readonly DamageableItemsDatabase _database;
        private readonly NavigationGrid _navigationGrid;

        private readonly Dictionary<CreatureType, DamageableItemConfig> _itemConfigs =
            new Dictionary<CreatureType, DamageableItemConfig>();

        private readonly List<Vector3> _candidatePositions = new List<Vector3>(4);

        private Filter _playerFilter;
        private Stash<PositionComponent> _positionStash;
        private float _elapsedTime;

        public DamageableItemsSpawnSystem(
            DamageableItemsFactory damageableItemsFactory,
            DamageableItemsDatabase damageableItemsDatabase,
            NavigationGrid navigationGrid)
        {
            _damageableItemsFactory = damageableItemsFactory;
            _database = damageableItemsDatabase;
            _navigationGrid = navigationGrid;

            InitializeItemConfigs();
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _playerFilter = World.Filter
                .With<PlayerTag>()
                .With<PositionComponent>()
                .Without<DeathComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_database == null || _damageableItemsFactory == null || _navigationGrid == null)
                return;

            if (TryGetPlayerPosition(out Vector3 playerPosition) == false)
                return;

            _elapsedTime += deltaTime;

            foreach (var item in _itemConfigs.Values)
            {
                if (_elapsedTime >= item.SpawnInterval)
                {
                    if (TryFindSpawnPosition(playerPosition, item, out Vector3 spawnPosition))
                    {
                        _damageableItemsFactory.Create(item, spawnPosition);
                        _elapsedTime = 0;
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        private void InitializeItemConfigs()
        {
            if (_database == null || _database.Items.Count == 0)
                throw new ArgumentNullException(nameof(_database));

            foreach (var config in _database.Items)
            {
                if (config == null)
                    throw new ArgumentNullException(nameof(_database));

                if (_itemConfigs.ContainsKey(config.Type))
                {
                    UnityEngine.Debug.LogWarning(
                        $"EnemySpawnSystem: duplicate config for " +
                        $"{config.Type}. The first config will be used.",
                        config);

                    continue;
                }

                _itemConfigs.Add(config.Type, config);
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

        private bool TryFindSpawnPosition(Vector3 playerPosition, DamageableItemConfig itemConfig, out Vector3 spawnPosition)
        {
            ColliderConfig colliderConfig = itemConfig.HitBox;
            ColliderComponent collider = new ColliderComponent
            {
                Type = colliderConfig.Type,
                Radius = colliderConfig.Radius,
                Length = colliderConfig.Length,
                Size = colliderConfig.Size,
                Center = colliderConfig.Center,
                Angle = 0f,
                CapsuleOrientation = colliderConfig.CapsuleOrientation,
            };

            _candidatePositions.Clear();

            AddCardinalCandidates(playerPosition, itemConfig.SpawnDistance);

            for (int i = 0; i < _candidatePositions.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, _candidatePositions.Count);
                Vector3 temp = _candidatePositions[i];

                _candidatePositions[i] = _candidatePositions[randomIndex];
                _candidatePositions[randomIndex] = temp;
            }

            foreach (Vector3 candidate in _candidatePositions)
            {
                if (_navigationGrid.IsWalkable(candidate, collider) == false)
                    continue;

                spawnPosition = candidate;
                return true;
            }

            spawnPosition = Vector3.zero;
            return true;
        }

        private void AddCardinalCandidates(Vector3 playerPosition, float spawnDistance)
        {
            Vector3[] directions =
            {
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.right,
            };

            foreach (Vector3 direction in directions)
            {
                Vector3 perpendicular = new Vector3(
                    -direction.y,
                    direction.x,
                    0f);
                float lateralOffset = UnityEngine.Random.Range(
                    -spawnDistance * CardinalLateralSpread,
                    spawnDistance * CardinalLateralSpread);
                float spawnDistance1 = spawnDistance;
                Vector3 candidate =
                    playerPosition +
                    (direction * spawnDistance1) +
                    (perpendicular * lateralOffset);

                if (_navigationGrid.IsWalkable(candidate) == false)
                    continue;

                _candidatePositions.Add(candidate);
            }
        }
    }
}