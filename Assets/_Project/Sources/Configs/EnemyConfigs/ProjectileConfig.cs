using Configs;
using ECS.WeaponComponents;
using Gameplay.View;
using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Game/Projectile/Projectile Config")]
    public class ProjectileConfig : ScriptableObject
    {
        public WeaponType WeaponType;
        public AnimatedView Prefab;
        public int PoolCapacity;
        public ColliderConfig Collider;
    }
}