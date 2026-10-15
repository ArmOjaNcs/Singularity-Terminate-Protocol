using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public interface IProjectileCreator
    {
        void Configure(Entity weapon, Entity projectile, Vector3 direction);
    }
}