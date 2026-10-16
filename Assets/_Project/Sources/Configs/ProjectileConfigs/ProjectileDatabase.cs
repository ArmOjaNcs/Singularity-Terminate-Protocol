using ECS.WeaponComponents;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectileConfigs
{
    [CreateAssetMenu(fileName = "ProjectileDatabase", menuName = "Game/Projectile/Projectile Database")]
    public class ProjectileDatabase : ScriptableObject
    {
        public List<ProjectileConfig> Projectiles;

        public ProjectileConfig Get(WeaponType type)
        {
            foreach (ProjectileConfig projectile in Projectiles)
            {
                if (projectile != null && projectile.WeaponType == type)
                    return projectile;
            }

            return null;
        }
    }
}