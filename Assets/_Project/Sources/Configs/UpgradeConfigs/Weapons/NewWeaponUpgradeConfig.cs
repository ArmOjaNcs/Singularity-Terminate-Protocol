using ECS.WeaponComponents;
using UnityEngine;
using WeaponConfigs;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "NewWeaponUpgradeConfig", menuName = "Game/Upgrades/New Weapon Upgrade Config")]
    public class NewWeaponUpgradeConfig : ExtendedUpgradeConfig
    {
        public WeaponType Type;
        public WeaponConfig Config;
    }
}