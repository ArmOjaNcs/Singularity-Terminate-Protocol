using Scellecs.Morpeh;

namespace ECS.ProjectileComponents
{
    public struct ProjectileCreationComponent : IComponent
    {
        public float ElapsedTime;
        public int RemainingCount;
        public int ShotsCreatedInCycle;
        public bool IsInitialized;
        public bool IsWaitingForCycle;
        public bool IsComboRestart;
    }
}
