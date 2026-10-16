using ECS.PlayerComponents;
using ECS.WeaponComponents;
using ECS.WeaponSystems;
using Scellecs.Morpeh;
using UnityEngine;
using WeaponConfigs;
using Zenject;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "NewWeaponUpgradeConfig", menuName = "Game/Upgrades/New Weapon Upgrade Config")]
    public class NewWeaponUpgradeConfig : UpgradeConfig
    {
        public WeaponType Type;
        public WeaponConfig Config;
        [Inject] protected WeaponFactory _factory;

        public override void Apply(Entity entity, PlayerInventoryComponent inventory)
        {
            World world = entity.GetWorld();

            _factory.Create(Config, entity);

            inventory.ActiveUpgrades.Add(UpgradeId, 1);
            inventory.CurrentWeaponsAmount++;
        }
    }
}