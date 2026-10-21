using System;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using PlayerConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "SkillUpgradeConfig", menuName = "Game/Upgrades/Skill Upgrade Config")]
    public class SkillUpgradeConfig : UpgradeConfig
    {
        public int MaxLevel = 5;
        public PlayerStats Stat;
        public float StatFactor = 5;
    }
}