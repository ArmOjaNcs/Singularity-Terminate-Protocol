using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "WeaponTestDummyConfig", menuName = "Game/Weapon Testing/Dummy Config")]
    public sealed class WeaponTestDummyConfig : CreatureConfig
    {
        [Min(0f)] public float RecoveryDelay = 1f;
        [Min(0f)] public float ReturnSpeed = 2f;
    }
}
