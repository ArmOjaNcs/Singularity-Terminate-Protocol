using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "VoltAttackConfig", menuName = "Game/Attack/Volt Attack Config")]
    public sealed class VoltAttackConfig : AttackConfig
    {
        [SerializeField, Min(1)] private int _hitsPerEmpWave = 1;

        internal override int HitsPerEmpWave => _hitsPerEmpWave;
    }
}
