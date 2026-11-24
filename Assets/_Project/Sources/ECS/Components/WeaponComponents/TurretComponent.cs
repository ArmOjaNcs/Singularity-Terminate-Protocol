using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.WeaponComponents
{
    internal struct TurretComponent : IComponent
    {
        public Entity Deployment;
        public int Generation;
        public float LifetimeRemaining;
        public bool HasUpdatedAim;
        public Vector3 FallbackDirection;
        public TurretView View;
    }
}
