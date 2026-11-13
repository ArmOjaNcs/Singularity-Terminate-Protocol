using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    [System.Serializable]
    public struct AttackPatternComponent : IComponent
    {
        public float SpreadAngle;
        public bool RandomDirectionFallback;
        public bool SelectTargetPerCycle;
    }
}
