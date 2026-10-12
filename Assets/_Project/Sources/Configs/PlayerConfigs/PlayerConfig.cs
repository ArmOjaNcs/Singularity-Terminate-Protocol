using Configs;
using UnityEngine;

namespace PlayerConfigs
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Player/Player Config")]
    public class PlayerConfig : CreatureConfig
    {
        public int MaxWeapons;
        public float EvasionChance;
        public float GatheringRadius;
    }
}