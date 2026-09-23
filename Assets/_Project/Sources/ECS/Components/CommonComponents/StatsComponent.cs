using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    [System.Serializable]
    public struct StatsComponent : IComponent
    {
        public float MaxHealth;
        public float HealthRegeneration;
        public float Damage;
        public float Luck;
        public float Defence;
        public float Speed;
    }
}