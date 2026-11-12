using System.Collections.Generic;
using Configs;
using Gameplay.View;
using UnityEngine;

namespace DamageableConfigs
{
    [CreateAssetMenu(fileName = "DamageableItem", menuName = "Game/Damageable Items/Item")]
    public class DamageableItemConfig : ScriptableObject
    {
        public DamageableItemType Type;
        public int Health;

        [Header("Spawn Params")]
        public float SpawnInterval;
        [Min(0f)] public float SpawnDistance = 12f;

        public ColliderConfig HitBox;
        public List<LootDropItem> DropItems;

        public AnimatedCreature Prefab;
        public int PoolCapacity;
    }
}