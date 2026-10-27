using System;
using System.Linq;
using ECS.AttackComponents;
using ECS.PlayerComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "WeaponStatsUpgradeConfig", menuName = "Game/Upgrades/Weapon Stats Upgrade Config")]
    public class WeaponUpgradeConfig : UpgradeConfig
    {
        public WeaponType Type;
        public WeaponStats Stat;
        public int StatsFactor = 5;
    }
}