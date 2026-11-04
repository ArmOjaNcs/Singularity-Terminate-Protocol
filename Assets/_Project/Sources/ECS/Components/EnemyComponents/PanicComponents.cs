using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.EnemyComponents
{
    public struct PanicRequest : IComponent
    {
        public Entity Target;
        public float Distance;
        public float SpeedMultiplier;
        public float Duration;
    }

    public struct EnemyPanicMovementComponent : IComponent
    {
        public bool IsActive;
        public Vector3 Destination;
        public float SpeedMultiplier;
        public float RemainingDuration;
    }
}
