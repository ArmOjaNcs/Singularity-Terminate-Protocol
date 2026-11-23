using Scellecs.Morpeh;

namespace ECS.WeaponComponents
{
    public struct WeaponComponent : IComponent
    {
        public Entity Owner;
        public int Level;
        public WeaponType Type;
    }
}