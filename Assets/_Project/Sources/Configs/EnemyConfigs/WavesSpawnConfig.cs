using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EnemyConfigs
{
    [CreateAssetMenu(fileName = "WavesSpawnConfig", menuName = "Game/Enemy/Waves Spawn Config")]
    public class WavesSpawnConfig : ScriptableObject
    {
        public List<EnemyWaveSpawnConfig> Waves;

        [Header("Spawn Position")]
        [Min(0f)] public float MinSpawnDistance = 8f;
        [Min(0f)] public float MaxSpawnDistance = 12f;

        [Min(1)] public int PositionAttempts = 20;
        [Min(0.1f)] public float RetryInterval = 1f;
    }
}