using ECS.WeaponComponents;
using UnityEngine;
using WeaponConfigs;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "NewWeaponUpgradeConfig", menuName = "Game/Upgrades/New Weapon Upgrade Config")]
    public class NewWeaponUpgradeConfig : UpgradeConfig
    {
        public WeaponType Type;
        public WeaponConfig Config;
    }
}