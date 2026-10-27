using ECS.CommonComponents;
using ECS.ProjectileComponents;
using UnityEngine;

namespace Configs
{
    [System.Serializable]
    public class ColliderConfig
    {
        public ColliderType Type;
        public float Radius;
        public float Length;
        public Vector2 Size;
        public Vector2 Center;
        public CapsuleOrientation CapsuleOrientation;
    }
}