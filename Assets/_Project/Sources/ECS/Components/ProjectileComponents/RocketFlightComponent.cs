using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct RocketFlightComponent : IComponent
    {
        public Entity Target;
        public Vector3 Destination;
        public Vector3 Direction;
        public float Speed;
        public float MaxDistance;
        public float TravelledDistance;
        public float TurnSpeed;
        public float ExplosionDuration;
        public bool HomingEnabled;
    }
}
