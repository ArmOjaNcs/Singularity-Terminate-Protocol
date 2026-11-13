using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.AttackComponents
{
    public struct AttackSequenceComponent : IComponent
    {
        public int Id;
        public bool IsActive;
        public Entity LockedTarget;
        public Entity AttackTarget;
        public Vector3 FallbackDirection;
        public bool HasFallbackDirection;
        public int UsedSectorMask;
        public int AssignedSectorCycle;
        public int AssignedSectorIndex;
        public bool AxeSweepClockwise;
        public bool HasAxeSweepDirection;
        public List<Entity> VisitedTargets;
        public int ReleasesCreated;
        public int OutstandingProjectiles;
        public int RicochetsCount;
        public bool RicochetPending;
        public Entity PendingTarget;
    }
}
