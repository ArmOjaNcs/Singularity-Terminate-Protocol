using Scellecs.Morpeh;

namespace ECS.WeaponComponents
{
    public struct WeaponComponent : IComponent
    {
        public int Level;
        public float Damage;
        public float Cooldown;
        public float Area;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public int ProjectileCount;
        public int Pierce;
    }
}