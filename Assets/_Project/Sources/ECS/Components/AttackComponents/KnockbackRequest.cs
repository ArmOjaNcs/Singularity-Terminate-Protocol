using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonComponents
{
    public struct KnockbackRequest : IComponent
    {
        public Entity Source;
        public Entity Target;
        public Vector3 Direction;
        public float Force;
    }
}