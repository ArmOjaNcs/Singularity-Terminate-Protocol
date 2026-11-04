using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.AttackComponents
{
    public struct AttackSequenceComponent : IComponent
    {
        public int Id;
        public bool IsActive;
        public Entity LockedTarget;
        public List<Entity> VisitedTargets;
        public int ReleasesCreated;
        public int OutstandingProjectiles;
        public int RicochetsCount;
        public bool RicochetPending;
        public Entity PendingTarget;
    }
}
