using System.Collections.Generic;
using UnityEngine;

namespace ECS.CommonComponents
{
    public static class TraumaStatusUtility
    {
        private const float MinimumMultiplier = 0.05f;

        public static float GetMovementMultiplier(
            List<TraumaInstance> instances)
        {
            return GetMultiplier(instances, useMovementSlow: true);
        }

        public static float GetAttackDamageMultiplier(
            List<TraumaInstance> instances)
        {
            return GetMultiplier(instances, useMovementSlow: false);
        }

        private static float GetMultiplier(
            List<TraumaInstance> instances,
            bool useMovementSlow)
        {
            if (instances == null || instances.Count == 0)
                return 1f;

            float multiplier = 1f;

            for (int i = 0; i < instances.Count; i++)
            {
                float reduction = useMovementSlow
                    ? instances[i].MovementSlowPercent
                    : instances[i].AttackDamageReductionPercent;

                multiplier *= 1f - Mathf.Clamp(reduction, 0f, 100f) / 100f;
            }

            return Mathf.Max(MinimumMultiplier, multiplier);
        }
    }
}
