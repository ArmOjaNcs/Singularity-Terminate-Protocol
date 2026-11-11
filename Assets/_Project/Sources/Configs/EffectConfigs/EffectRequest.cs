using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.EffectComponents
{
    public struct EffectRequest : IComponent
    {
        public Entity Target;
        public DamageContext Context;
        public float AppliedDamage;
        public Vector3 ImpactPosition;
        public bool HasImpactPosition;
    }
}
