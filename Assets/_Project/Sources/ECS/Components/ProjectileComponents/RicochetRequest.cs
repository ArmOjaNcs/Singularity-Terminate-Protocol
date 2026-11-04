using ECS.AttackComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct RicochetRequest : IComponent
    {
        public Entity Projectile;
        public Entity Weapon;
        public Entity HitTarget;
        public int SequenceId;
        public ProjectileSequencePolicy SequencePolicy;
        public Vector3 HitPosition;
    }
}
