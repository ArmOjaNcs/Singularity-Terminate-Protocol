using ECS.PlayerComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "NewTurretUpgradeConfig", menuName = "Game/Upgrades/New Turret Config")]
    public class NewTurretUpgradeConfig : NewWeaponUpgradeConfig
    {
        public override void Apply(Entity entity, PlayerInventoryComponent inventory)
        {
            World world = entity.GetWorld();

            _factory.Create(Config, entity);

            inventory.ActiveUpgrades.Add(UpgradeId, 1);
            inventory.CurrentTurretsAmount++;
        }
    }
}