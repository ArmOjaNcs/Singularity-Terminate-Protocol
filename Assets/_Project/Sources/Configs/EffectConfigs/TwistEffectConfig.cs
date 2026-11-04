using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(
        fileName = "TwistEffectConfig",
        menuName = "Game/Effects/Twist Effect Config")]
    public class TwistEffectConfig : EffectConfig
    {
        [Min(0f)] public float ShakeAmplitude = 0.12f;
        [Min(0.1f)] public float ShakeFrequency = 12f;
        [Min(0.01f)] public float RefreshDuration = 0.45f;
    }
}
