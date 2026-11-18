using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct SwordThrustComponent : IComponent
    {
        public Entity Owner;
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public bool FollowOwner;
        public float Duration;
        public float CurrentTime;
    }
}
