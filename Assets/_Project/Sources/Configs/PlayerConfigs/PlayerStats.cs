using Configs;

namespace PlayerConfigs
{
    [System.Serializable]
    public struct PlayerStats
    {
        public BaseStats BaseStats;
        public int MaxWeapons;
        public float EvasionChance;
        public float GatheringRadius;
    }
}