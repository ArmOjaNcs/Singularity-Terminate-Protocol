using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    [System.Serializable]
    public struct TrajectoryComponent : IComponent
    {
        public float Duration;
        public float FlightDistance;
        public float FlightDistanceTolerance;
        public float ZigZagAmplitude;
        public float ZigZagFrequency;
        public float ShakeAmplitude;
        public float ShakeFrequency;
    }
}
