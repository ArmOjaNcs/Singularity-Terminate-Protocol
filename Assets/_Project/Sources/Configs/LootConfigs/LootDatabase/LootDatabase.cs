using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootDatabase", menuName = "Game/Loot/Loot Database")]
public class LootDatabase : ScriptableObject
{
    public List<LootDropItem> LootItems;
}
