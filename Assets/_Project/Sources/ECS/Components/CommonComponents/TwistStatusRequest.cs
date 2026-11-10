using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct TwistStatusRequest : IComponent
    {
        public Entity Target;
        public Entity SourceWeapon;
        public float ShakeAmplitude;
        public float ShakeFrequency;
        public float Duration;
    }
}
