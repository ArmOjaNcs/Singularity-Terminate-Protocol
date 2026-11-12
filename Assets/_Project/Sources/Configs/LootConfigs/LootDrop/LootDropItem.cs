using Gameplay.View;
using UnityEngine;

[CreateAssetMenu(fileName = "LootDropItem", menuName = "Game/Loot/Loot Drop Item")]
public class LootDropItem : ScriptableObject
{
    public string Id;

    public ItemTypes Type;

    public EntityView Prefab;
    public int PoolCapacity;
}
