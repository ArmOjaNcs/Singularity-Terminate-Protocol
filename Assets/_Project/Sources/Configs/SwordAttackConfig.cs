using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "SwordAttackConfig", menuName = "Game/Attack/Sword Attack Config")]
    public sealed class SwordAttackConfig : AttackConfig
    {
        [SerializeField, Min(0f)] private float _thrustDistance = 1f;
        [SerializeField, Min(0f)] private float _zigZagWidth = 1f;

        internal override float ThrustDistance => _thrustDistance;
        internal override float ZigZagWidth => _zigZagWidth;
    }
}
