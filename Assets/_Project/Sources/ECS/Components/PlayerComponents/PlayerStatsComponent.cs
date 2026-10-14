using Scellecs.Morpeh;

namespace ECS.PlayerComponents
{
    [System.Serializable]
    public struct PlayerStatsComponent : IComponent
    {
        public float EvasionChance;
        public float GatheringRadius;
    }
}