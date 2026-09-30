using Gameplay.View;
using UnityEngine;

namespace EnemyConfigs
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Game/Projectile/Projectile Config")]
    public class ProjectileConfig : ScriptableObject
    {
        public AnimatedView Prefab;
        public int PoolCapacity = 10;
        public float Speed = 5f;
        public float Lifetime = 3f;
        public float Radius = 0.25f;
    }
}