using System.Collections.Generic;
using Scellecs.Morpeh;
using UnityEngine;

namespace UpgradeConfigs
{
    public class UpgradeConfig : ScriptableObject
    {
        public string UpgradeId;
        public string Title;
        [TextArea(3, 5)]
        public string Description;
        public Sprite Icon;
        public List<UpgradeConfig> RequiredUpgradeIds;
    }
}