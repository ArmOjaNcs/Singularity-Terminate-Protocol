using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;

namespace ECS.CommonComponents
{
    public struct DamageContext
    {
        public Entity Weapon;
        public DamageType DamageType;
        public CreatureType SourceType;
        public WeaponType WeaponType;
        public ProjectileType ProjectileType;
    }
}