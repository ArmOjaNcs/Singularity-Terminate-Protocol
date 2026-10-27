using Configs;
using ECS.EnemyComponents;
using UnityEngine;

namespace EnemyConfigs
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy/Enemy Config")]
    public class EnemyConfig : CreatureConfig
    {
        public int PoolCapacity;
        public MovePolicyType MovePolicy;
        public AttackPolicyType AttackPolicy;
        [Min(0f)] public float PreferredAttackDistance;
    }
}
