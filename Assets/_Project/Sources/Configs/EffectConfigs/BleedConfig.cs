using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(fileName = "BleedConfig", menuName = "Game/Effect/Bleed Config")]
    public class BleedConfig : EffectConfig
    {
        public int BleedStack;
        public float DamagePerSecond;
        public float Duration;
    }
}