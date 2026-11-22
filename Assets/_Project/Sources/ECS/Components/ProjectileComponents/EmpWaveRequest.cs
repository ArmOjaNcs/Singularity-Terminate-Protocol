using ECS.AttackComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct EmpWaveRequest : IComponent
    {
        public Entity Weapon;
        public Vector3 Position;
        public float Damage;
        public AttackTargetType[] TargetTypes;
    }
}
