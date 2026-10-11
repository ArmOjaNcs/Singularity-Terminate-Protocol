using ECS.ProjectileComponents;
using System;
using UnityEngine;

namespace Core.Game
{
    public static class CombatSettings
    {
        public const float MinAttackCooldown = 0.05f;
        public const float MinProjectileLifetime = 1f;
        public const float MinProjectileRadius = 0.01f;

        public const int MinProjectileCount = 1;
        public const int MinPoolCapacity = 1;

        public static float GetSafeAttackCooldown(float value)
        {
            return Mathf.Max(value, MinAttackCooldown);
        }

        public static float GetSafeProjectileLifetime(float value)
        {
            return Mathf.Max(value, MinProjectileLifetime);
        }

        public static float GetSafeProjectileRadius(float value)
        {
            return Mathf.Max(value, MinProjectileRadius);
        }

        public static float GetSafeProjectileSpeed(float value)
        {
            return Mathf.Max(value, 0f);
        }

        public static float GetSafeDamage(float value)
        {
            return Mathf.Max(value, 0f);
        }

        public static float GetSafeKnockback(float value)
        {
            return Mathf.Max(value, 0f);
        }

        public static float GetSafeDistance(float value)
        {
            return Mathf.Max(value, 0f);
        }

        public static float GetSafeTargetSearchRadius(float value)
        {
            return Mathf.Max(value, 0f);
        }

        public static float GetSafeAttackDuration(float value)
        {
            return Mathf.Max(value, 0f);
        }

        public static int GetSafeProjectileCount(int value)
        {
            return Mathf.Max(value, MinProjectileCount);
        }

        public static int GetSafePierce(int value)
        {
            return Mathf.Max(value, 0);
        }

        public static int GetSafePoolCapacity(int value)
        {
            return Mathf.Max(value, MinPoolCapacity);
        }

        public static int GetMaxHits(int pierce)
        {
            return GetSafePierce(pierce) + 1;
        }

        public static ProjectileType GetSafeProjectileType(
            ProjectileType type,
            ProjectileType fallback)
        {
            if (Enum.IsDefined(typeof(ProjectileType), type))
                return type;

            return fallback;
        }
    }
}