using Scellecs.Morpeh;

namespace ECS.ProjectileComponents
{
    public struct ProjectileCreationComponent : IComponent
    {
        public float ElapsedTime;
        public int RemainingCount;
        public bool IsInitialized;
    }
}