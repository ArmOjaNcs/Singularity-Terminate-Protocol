using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "BaseGameObjectConfig", 
        menuName = "Game/GameObjectConfig/BaseGameObjectConfig")]
    public class BaseGameObjectConfig : ScriptableObject
    {
        public GameObject Prefab;
        public ColliderConfig Collider;
    }
}