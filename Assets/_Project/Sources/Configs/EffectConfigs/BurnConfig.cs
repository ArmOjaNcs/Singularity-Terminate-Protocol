using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(fileName = "BurnConfig", menuName = "Game/Effects/Burn Config")]
    public class BurnConfig : EffectConfig
    {
        [Min(1)] public int MaxStacks = 5;
        [Min(0f)] public float DamagePerStackPerSecond = 1f;
        [Min(0f)] public float Duration = 3f;

        [Min(0.1f)] public float BasePanicInterval = 1.5f;
        [Min(0.1f)] public float MinPanicInterval = 0.4f;
        [Min(0f)] public float PanicDistance = 1.5f;
        [Min(1f)] public float PanicSpeedMultiplier = 1.75f;
        [Min(0.1f)] public float PanicBurstDuration = 1.25f;
    }
}
