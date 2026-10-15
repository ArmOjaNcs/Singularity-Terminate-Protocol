using Scellecs.Morpeh;
using System.Collections.Generic;

namespace ECS.ProjectileComponents
{
    public struct ProjectileHitComponent : IComponent
    {
        public List<ProjectileHitRecord> Records;
        public int HitCount;
    }
}