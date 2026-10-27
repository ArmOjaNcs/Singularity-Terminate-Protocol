using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("Collider")]
        public ColliderConfig HitBox;
        public ColliderConfig NavigationBox;
    }
}