using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct CleanupRequest : IComponent
    {
        public Entity Candidate;
    }
}