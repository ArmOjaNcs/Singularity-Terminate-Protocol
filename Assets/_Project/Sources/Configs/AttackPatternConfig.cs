using System;
using UnityEngine;

namespace Configs
{
    [Serializable]
    public struct AttackPatternConfig
    {
        [Min(0f)] public float SpreadAngle;
        public bool RandomDirectionFallback;
        public bool SelectTargetPerCycle;
    }
}
