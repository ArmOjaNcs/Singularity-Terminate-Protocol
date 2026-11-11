using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.DamageComponents
{
    public struct DamageRequest : IComponent
    {
        public Entity Target;
        public float Damage;
        public DamageContext Context;
        public Vector3 ImpactPosition;
        public bool HasImpactPosition;
        public bool ApplyTwistOnDamage;
        public float TwistShakeAmplitude;
        public float TwistShakeFrequency;
        public float TwistDuration;
    }
}
