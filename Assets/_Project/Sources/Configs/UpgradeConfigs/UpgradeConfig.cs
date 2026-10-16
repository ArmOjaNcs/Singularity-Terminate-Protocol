using System.Collections.Generic;
using ECS.PlayerComponents;
using Scellecs.Morpeh;
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
        public List<UpgradeConfig> RequiredUpgradeIds;

        public abstract void Apply(Entity entity, PlayerInventoryComponent inventory);
    }
}