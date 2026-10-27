using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonComponents
{
    [System.Serializable]
    public struct PositionComponent : IComponent
    {
        public Vector3 Position;
    }
}