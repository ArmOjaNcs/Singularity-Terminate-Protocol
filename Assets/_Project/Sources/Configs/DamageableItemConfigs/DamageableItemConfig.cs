using System.Collections.Generic;
using Gameplay.View;
using UnityEngine;

namespace DamageableConfigs
{
    [CreateAssetMenu(fileName = "DamageableItem", menuName = "Game/Damageable Items/Item")]
    public class DamageableItemConfig : ColliderEntityConfig
    {
        public CreatureType Type;
        public int Health;

        [Header("Spawn Params")]
        public float SpawnInterval;
        [Min(0f)] public float SpawnDistance = 12f;
        public AnimatedCreature Prefab;
        public int PoolCapacity;
    }
}