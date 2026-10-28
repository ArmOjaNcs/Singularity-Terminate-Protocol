using UnityEngine;

[CreateAssetMenu(fileName = "LootDropItem", menuName = "Game/Loot/Loot Drop Item")]
public class LootDropItem : ScriptableObject
{
    public string Id;

    public ItemTypes Type;

    public GameObject Prefab;
}
