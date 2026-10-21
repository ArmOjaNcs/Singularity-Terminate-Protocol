using System.Collections.Generic;
using UnityEngine;

namespace EnemyConfigs
{
    [CreateAssetMenu(fileName = "EnemySpawnDatabase", menuName = "Game/Enemy/Enemy Spawn Database")]
    public class EnemySpawnDatabase : ScriptableObject
    {
        public List<EnemyWaveSpawnConfig> Waves;

        [Header("Spawn Position")]
        [Min(0f)] public float MinSpawnDistance = 8f;
        [Min(0f)] public float MaxSpawnDistance = 12f;

        [Min(1)] public int PositionAttempts = 20;
        [Min(0.1f)] public float RetryInterval = 1f;
    }
}