using ECS.WeaponComponents;
using Scellecs.Morpeh;
using UpgradeConfigs;

namespace ECS.PlayerComponents
{
    public struct UpgradeSelectedEvent : IComponent
    {
        public UpgradeConfig SelectedUpgrade;
        public string BaseWeaponId;
        public WeaponType BaseWeaponType;
    }
}