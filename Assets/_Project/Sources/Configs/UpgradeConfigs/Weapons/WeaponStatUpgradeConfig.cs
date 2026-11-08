using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "WeaponStatsUpgradeConfig", menuName = "Game/Upgrades/Weapon Stats Upgrade Config")]
    public class WeaponStatUpgradeConfig : UpgradeConfig
    {
        public WeaponStats Stat;
        public int Amount = 5;
    }
}