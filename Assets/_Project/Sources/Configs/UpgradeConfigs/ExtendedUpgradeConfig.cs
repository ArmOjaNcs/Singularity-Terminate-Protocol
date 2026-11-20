using UnityEngine;

namespace UpgradeConfigs
{
    public abstract class ExtendedUpgradeConfig : UpgradeConfig
    {
        public string Title;
        public Sprite Icon;
        public int MaxLevel = 5;
    }
}