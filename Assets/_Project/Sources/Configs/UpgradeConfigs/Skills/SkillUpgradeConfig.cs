using PlayerConfigs;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "SkillUpgradeConfig", menuName = "Game/Upgrades/Skill Upgrade Config")]
    public class SkillUpgradeConfig : UpgradeConfig
    {
        public PlayerStats Stat;
        public float StatFactor = 5;
    }
}