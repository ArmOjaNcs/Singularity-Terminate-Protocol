using Configs;
using ECS.WeaponComponents;
using EffectConfigs;
using UnityEngine;

namespace WeaponConfigs
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Game/Weapon/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        public WeaponType Type;
        public AttackConfig AttackConfig;
        public EffectConfig[] Effects;
    }
}