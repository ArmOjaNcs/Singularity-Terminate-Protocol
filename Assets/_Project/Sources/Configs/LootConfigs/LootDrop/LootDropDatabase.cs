using System;
using System.Collections.Generic;
using UnityEngine;

namespace LootConfigs
{
    [CreateAssetMenu(fileName = "LootDropDatabase", menuName = "Game/Loot Drop Database")]
    public class LootDropDatabase : ScriptableObject
    {
        [SerializeField] private List<LootTableEntry> _tables;

        private Dictionary<CreatureType, List<PossibleDropsEntry>> _runtimeTables;

        public void Initialize()
        {
            _runtimeTables = new Dictionary<CreatureType, List<PossibleDropsEntry>>();

            foreach (var entry in _tables)
            {
                if (_runtimeTables.ContainsKey(entry.EnemyType) == false)
                    _runtimeTables.Add(entry.EnemyType, entry.Drops);
            }
        }

        public List<PossibleDropsEntry> GetLootForEnemy(CreatureType type)
        {
            if (_runtimeTables == null)
                Initialize();

            return _runtimeTables.TryGetValue(type, out var list) ? list : null;
        }
    }
}