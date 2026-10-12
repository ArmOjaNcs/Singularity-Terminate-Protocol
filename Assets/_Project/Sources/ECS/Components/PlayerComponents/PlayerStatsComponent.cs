using Scellecs.Morpeh;

namespace ECS.PlayerComponents
{
    [System.Serializable]
    public struct PlayerStatsComponent : IComponent
    {
        public int MaxWeapons;
        public float EvasionChance;
        public float GatheringRadius;
    }
}