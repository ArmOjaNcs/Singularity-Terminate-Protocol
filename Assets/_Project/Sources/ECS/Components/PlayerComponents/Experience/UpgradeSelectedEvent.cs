using Scellecs.Morpeh;
using UpgradeConfigs;

namespace ECS.PlayerComponents
{
    public struct UpgradeSelectedEvent : IComponent
    {
        public UpgradeConfig SelectedUpgrade;
    }
}