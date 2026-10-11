using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonComponents
{
    public struct KnockbackComponent : IComponent
    {
        public Vector3 Direction;
        public float Force;
        public float Duration;
        public float Elapsed;
    }
}