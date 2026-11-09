using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct BattleStatisticComponent : IComponent
    {
        public Entity Target;
        public DamageContext Context;
        public float Damage;
        public float BlockedDamage;
        public bool IsCritical;
        public bool IsKilledEnemy;
        public int GainedCredits;
    }
}