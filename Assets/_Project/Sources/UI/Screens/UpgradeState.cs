using ECS.WeaponComponents;
using UnityEngine;
using UpgradeConfigs;

namespace UI
{
    public struct UpgradeState
    {
        public string Title;
        public string Description;
        public Sprite Icon;
        public int CurrentLevel;
        public UpgradeConfig Config;
        public string BaseWeaponId;
        public WeaponType BaseWeaponType;
    }
}