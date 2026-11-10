using ECS.CommonComponents;
using Scellecs.Morpeh;

namespace ECS.DamageComponents
{
    public struct DamageRequest : IComponent
    {
        public Entity Target;
        public float Damage;
        public DamageContext Context;
        public bool ApplyTwistOnDamage;
        public float TwistShakeAmplitude;
        public float TwistShakeFrequency;
        public float TwistDuration;
    }
}
