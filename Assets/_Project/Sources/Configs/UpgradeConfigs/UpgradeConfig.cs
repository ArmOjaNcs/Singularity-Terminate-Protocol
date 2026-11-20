using System.Collections.Generic;
using UnityEngine;

namespace UpgradeConfigs
{
    public abstract class UpgradeConfig : ScriptableObject
    {
        public string UpgradeId;
        [TextArea(3, 5)]
        public string Description;
        public UpgradeType UpgradeType;
        public List<UpgradeConfig> RequiredUpgradeIds;
    }
}