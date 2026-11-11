using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct GrenadeFlightComponent : IComponent
    {
        public Vector3 StartPosition;
        public Vector3 LandingPosition;
        public Vector3 LateralDirection;
        public float ArcOffset;
        public float Duration;
        public float Elapsed;
        public float ExplosionDuration;
    }
}
