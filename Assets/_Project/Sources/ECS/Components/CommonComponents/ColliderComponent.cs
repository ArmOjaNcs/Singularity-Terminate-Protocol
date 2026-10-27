using ECS.ProjectileComponents;
using UnityEngine;

namespace ECS.CommonComponents
{
    public struct ColliderComponent
    {
        public ColliderType Type;
        
        public float Radius;
        public float Length;
        public Vector2 Size;
        public Vector2 Center;
        public float Angle;
        public CapsuleOrientation CapsuleOrientation;
    }
}
