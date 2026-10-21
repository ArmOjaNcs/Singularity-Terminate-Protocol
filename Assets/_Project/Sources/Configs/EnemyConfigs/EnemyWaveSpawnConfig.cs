using System.Collections.Generic;

namespace EnemyConfigs
{
    [System.Serializable]
    public class EnemyWaveSpawnConfig
    {
        public float StartTime;
        public List<EnemySpawnConfig> Enemies;
    }
}