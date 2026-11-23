using System;
using Scellecs.Morpeh;

namespace ECS.AttackComponents
{
    public struct VoltAttackComponent : IComponent
    {
        public int HitsPerEmpWave;
        private int _hitsUntilEmpWave;

        internal bool RegisterHit()
        {
            if (_hitsUntilEmpWave > 0)
            {
                _hitsUntilEmpWave--;
                return false;
            }

            _hitsUntilEmpWave = Math.Max(1, HitsPerEmpWave) - 1;
            return true;
        }
    }
}
