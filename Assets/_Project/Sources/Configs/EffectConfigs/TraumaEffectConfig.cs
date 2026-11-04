using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(
        fileName = "TraumaEffectConfig",
        menuName = "Game/Effects/Trauma Effect Config")]
    public class TraumaEffectConfig : EffectConfig
    {
        [Min(0f)] public float Duration = 3f;
        [Range(0f, 100f)] public float MovementSlowPercent = 25f;
        [Range(0f, 100f)] public float AttackDamageReductionPercent = 25f;
    }
}