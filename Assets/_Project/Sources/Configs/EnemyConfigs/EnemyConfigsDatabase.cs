using System.Collections.Generic;
using UnityEngine;

namespace EnemyConfigs
{
    [CreateAssetMenu(fileName = "EnemyConfigsDatabase", menuName = "Game/Enemy/Enemy Configs Database")]
    public class EnemyConfigsDatabase : ScriptableObject
    {
        public List<EnemyConfig> Enemies;
    }
}