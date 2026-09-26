using Configs;
using UnityEngine;

namespace EnemyConfigs
{
    [CreateAssetMenu(
        fileName = "EnemyConfig",
        menuName = "Game/Enemy/Enemy Config"
    )]
    public class EnemyConfig : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public GameObject Prefab;
        public BaseStats Stats;
    }
}