using System.Collections.Generic;
using UnityEngine;

namespace DamageableConfigs
{
    [CreateAssetMenu(fileName = "DamageableItemsDatabase", menuName = "Game/Damageable Items/Database")]
    public class DamageableItemsDatabase : ScriptableObject
    {
        public List<DamageableItemConfig> Items;
    }
}