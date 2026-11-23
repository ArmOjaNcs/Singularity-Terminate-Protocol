using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileComponents
{
    public struct ShrapnelFragmentComponent : IComponent
    {
        public Entity SourceTarget;
        public Vector3 PreviousPosition;
    }
}
