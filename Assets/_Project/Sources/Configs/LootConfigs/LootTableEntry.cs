using System.Collections.Generic;

namespace LootConfigs
{
    [System.Serializable]
    public struct LootTableEntry
    {
        public CreatureType EnemyType;
        public List<PossibleDropsEntry> Drops;
    }
}