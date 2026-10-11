using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.ProjectileComponents
{
    public struct ProjectileHitComponent : IComponent
    {
        public List<Entity> Targets;
        public int HitCount;
    }
}