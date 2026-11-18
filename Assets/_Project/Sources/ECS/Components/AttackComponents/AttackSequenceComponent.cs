using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.AttackComponents
{
    public struct SwordAttackCycleState
    {
        public int SequenceId;
        public int CycleIndex;
        // Cardinal sword orientation before the thrust's diagonal tilt.
        public int SwordFacingSector;
        public int LastProjectileSwingIndex;
        public Vector3 LastProjectileEndPosition;
        public bool HasLastProjectileEndPosition;
        public Entity PendingRicochetTarget;
        public bool HasPendingRicochet;
    }

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
        public int SwordStartSector;
        public bool HasSwordStartSector;
        public int SwordUsedSectorMask;
        public bool SwordMainSeriesFinished;
        public bool SwordRicochetCycle;
        public Vector3 SwordRicochetOrigin;
        // Keeps each Cycle's ricochet target and endpoint tied to that Cycle.
        public List<SwordAttackCycleState> SwordCycles;
        public List<Entity> VisitedTargets;
        public int ReleasesCreated;
        public int OutstandingProjectiles;
        public int RicochetsCount;
        public bool RicochetPending;
        public Entity PendingTarget;
        public List<Entity> PendingRicochetTargets;
    }
}
