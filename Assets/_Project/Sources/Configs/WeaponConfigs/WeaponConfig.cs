using Configs;
using ECS.WeaponComponents;
using EffectConfigs;
using ProjectileConfigs;
using UnityEngine;

namespace WeaponConfigs
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Game/Weapon/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        public WeaponType Type;
        public ProjectileConfig ProjectileConfig;
        public AttackConfig AttackConfig;
        public EffectConfig[] Effects;
    }
}