using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.AttackComponents
{
    public struct RipperSequenceComponent : IComponent
    {
        public int SequenceId;
        public bool IsInitialized;
        public Vector3 AttackDirection;
    }
}
