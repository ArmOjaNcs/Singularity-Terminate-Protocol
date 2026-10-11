using UnityEngine;

namespace EffectConfigs
{
    [CreateAssetMenu(
        fileName = "KnockbackEffectConfig",
        menuName = "Game/Effects/Knockback Effect Config")]
    public class KnockbackEffectConfig : EffectConfig
    {
        public float Force;
    }
}