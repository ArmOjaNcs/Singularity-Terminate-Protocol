using ECS.AttackComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct ProjectileComponent : IComponent
    {
        public Entity Weapon;
        public ProjectileType Type;
        public WeaponType WeaponType;

        public float Speed;
        public float Lifetime;
        public float CurrentLifetime;
        public float Damage;
        public int Pierce;

        public Vector3 Direction;
        public AttackTargetType TargetType;

        public bool CleanupRequested;
    }
}