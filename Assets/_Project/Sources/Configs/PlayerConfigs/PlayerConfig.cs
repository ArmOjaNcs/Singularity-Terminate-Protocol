using UnityEngine;

namespace PlayerConfigs
{
    [CreateAssetMenu(
        fileName = "PlayerConfig",
        menuName = "Game/Player/Player Config"
    )]
    public class PlayerConfig : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public GameObject Prefab;
        public PlayerStats Stats;
    }
}