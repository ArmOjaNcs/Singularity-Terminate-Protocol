using System;
using UnityEngine;

namespace Configs
{
    [Serializable]
    public struct TrajectoryConfig
    {
        [Min(0f)] public float Duration;
        [Min(0f)] public float FlightDistance;
        [Min(0f)] public float FlightDistanceTolerance;
        [Min(0f)] public float ZigZagAmplitude;
        [Min(0f)] public float ZigZagFrequency;
        [Min(0f)] public float ShakeAmplitude;
        [Min(0f)] public float ShakeFrequency;
    }
}
