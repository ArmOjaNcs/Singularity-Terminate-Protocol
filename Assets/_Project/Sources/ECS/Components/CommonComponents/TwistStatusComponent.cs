using System.Collections.Generic;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonComponents
{
    public struct TwistStatusComponent : IComponent
    {
        public List<TwistInstance> Instances;
        public Vector3 VisualOffset;
    }

    public struct TwistInstance
    {
        public Entity SourceWeapon;
        public float RemainingDuration;
        public float Elapsed;
        public float ShakeAmplitude;
        public float ShakeFrequency;
        public float Phase;
    }
}
