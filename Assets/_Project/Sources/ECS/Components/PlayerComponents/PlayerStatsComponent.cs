using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.PlayerComponents
{
    [System.Serializable]
    public struct PlayerStatsComponent : IComponent
    {
        public StatsComponent Stats;
        public int MaxWeapons;
        public float GatheringRadius;
        public float EvasionChance;
    }
}