using UnityEngine;
using WeaponConfigs;

namespace Configs
{
    public class CreatureConfig : ScriptableObject
    {
        public CreatureType CreatureType;
        public string Id;
        public string Name;
        public GameObject Prefab;
        public BaseStats Stats;
        public WeaponConfig[] WeaponConfig;
        public ColliderConfig Collider;
    }
}