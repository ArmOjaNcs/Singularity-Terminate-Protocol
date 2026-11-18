using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct RipperTrajectoryComponent : IComponent
    {
        public int SequenceId;
        public int SwingIndex;
        public Vector3 AttackDirection;
        public Vector3 Direction;
        public Vector3 StartOffset;
        public float SweepLength;
        public float Duration;
        public float CurrentTime;
        public float ShakeAmplitude;
        public float ShakeFrequency;
        public bool MirrorY;
    }
}
