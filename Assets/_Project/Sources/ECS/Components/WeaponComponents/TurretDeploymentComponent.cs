using Scellecs.Morpeh;
using WeaponConfigs;

namespace ECS.WeaponComponents
{
    internal struct TurretDeploymentComponent : IComponent
    {
        public TurretWeaponConfig Config;
        public int ActiveCount;
        public int NextPlacement;
        public int Generation;
    }
}
