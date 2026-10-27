using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct ArcTrajectoryComponent : IComponent
    {
        public Vector3 Direction;
        public Vector3 ColliderOffset;
        public Vector2 BaseColliderCenter;
        public float DistanceFromSource;
        public float Radius;
        public float StartAngle;
        public float EndAngle;
        public float Duration;
        public float CurrentTime;
        public float StartRotationAngle;
        public float EndRotationAngle;
    }
}
