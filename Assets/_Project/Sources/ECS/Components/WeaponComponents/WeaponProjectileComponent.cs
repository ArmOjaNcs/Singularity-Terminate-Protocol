using ECS.ProjectileComponents;
using Scellecs.Morpeh;

namespace ECS.WeaponComponents
{
    public struct WeaponProjectileComponent : IComponent
    {
        public ProjectileType Type;
    }
}