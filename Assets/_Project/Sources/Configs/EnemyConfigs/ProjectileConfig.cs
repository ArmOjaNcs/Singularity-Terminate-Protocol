using Configs;
using ECS.ProjectileComponents;
using Gameplay.View;
using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Game/Projectile/Projectile Config")]
    public class ProjectileConfig : ScriptableObject
    {
        public ProjectileType Type;
        public AnimatedView Prefab;
        public int PoolCapacity;
        public ColliderConfig Collider;
    }
}