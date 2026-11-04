using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(fileName = "BleedConfig", menuName = "Game/Effects/Bleed Config")]
    public class BleedConfig : EffectConfig
    {
        [Min(1)] public int MaxStacks = 5;
        [Min(0f)] public float DamagePerStackPerSecond = 1f;
        public float Duration;
    }
}
