using System.Collections.Generic;
using UnityEngine;

namespace UpgradeConfigs
{
    public abstract class UpgradeConfig : ScriptableObject
    {
        public string UpgradeId;
        public string Title;
        [TextArea(3, 5)]
        public string Description;
        public Sprite Icon;
        public UpgradeType UpgradeType;
        public int MaxLevel = 5;
        public List<UpgradeConfig> RequiredUpgradeIds;
    }
}