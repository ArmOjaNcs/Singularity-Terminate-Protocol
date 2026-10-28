using UnityEngine;

namespace LootConfigs
{
    [CreateAssetMenu(fileName = "LootExpDropItem", menuName = "Game/Loot/Loot Exp Drop Item")]
    public class LootExpDropItem : LootDropItem
    {
        public int AddExpAmount;
    }
}