using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct ElectricTrajectoryComponent : IComponent
    {
        public Entity Target;
        public float Amplitude;
        public float Frequency;
        public float Phase;
        public Vector3 ForwardDirection;
        public Vector3 RicochetWaypoint;
        public bool IsDetouring;
    }
}
