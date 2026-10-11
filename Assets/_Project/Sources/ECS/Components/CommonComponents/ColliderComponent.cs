using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonComponents
{
    public struct ColliderComponent : IComponent
    {
        public ColliderType Type;
        
        public float Radius;
        public float Length;
        public Vector2 Size;
        public float Angle;
        public CapsuleOrientation CapsuleOrientation;
    }
}