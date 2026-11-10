using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(fileName = "ShockEffectConfig", menuName = "Game/Effects/Shock Effect Config")]
    public class ShockEffectConfig : EffectConfig
    {
        [Min(1)] public int MaxStacks = 5;
        [Min(0f)] public float DamagePerStackPerSecond = 1f;
        [Min(0f)] public float Duration = 3f;
        public TwistEffectConfig TwistOnDamage;
    }
}
