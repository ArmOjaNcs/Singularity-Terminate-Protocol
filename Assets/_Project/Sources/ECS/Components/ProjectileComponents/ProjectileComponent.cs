using ECS.AttackComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct ProjectileComponent : IComponent
    {
        public Entity Owner;
        public float Damage;
        public float Speed;
        public float Lifetime;
        public float CurrentLifetime;
        public float Radius;
        public Vector3 Direction;
        public AttackTargetType TargetType;
    }
}