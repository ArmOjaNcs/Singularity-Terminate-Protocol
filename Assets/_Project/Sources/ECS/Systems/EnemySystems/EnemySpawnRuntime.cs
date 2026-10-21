using EnemyConfigs;

namespace ECS.EnemySystems
{
    public sealed class EnemySpawnRuntime
    {
        public readonly EnemySpawnConfig Config;

        public int SpawnedCount;
        public float Timer;

        public EnemySpawnRuntime(EnemySpawnConfig config)
        {
            Config = config;
            SpawnedCount = 0;
            Timer = 0f;
        }
    }
}