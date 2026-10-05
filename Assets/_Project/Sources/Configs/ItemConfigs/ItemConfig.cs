using UnityEngine;

namespace Configs
{
    public abstract class ItemConfig : ScriptableObject
    {
        public string Id;
        public ItemTypes Type;
        public GameObject Prefab;
    }
}