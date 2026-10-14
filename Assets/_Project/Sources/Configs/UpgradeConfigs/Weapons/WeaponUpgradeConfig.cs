using ECS.WeaponComponents;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "WeaponUpgradeConfig", menuName = "Game/Upgrades/Weapon Upgrade Config")]
    public class WeaponUpgradeConfig : UpgradeConfig
    {
        public WeaponType Type;
        public bool IsBaseWeapon;
    }
}