using System.Collections.Generic;
using System.Linq;
using ECS.PlayerComponents;
using UI;
using UnityEngine;
using UpgradeConfigs;

public class UpgradeGenerator
{
    private readonly List<NewWeaponUpgradeConfig> _weapons;
    private readonly List<WeaponUpgradeConfig> _weaponUpgrades;

    private readonly List<NewTurretUpgradeConfig> _turrets;
    private readonly List<TurretUpgradeConfig> _turretUpgrades;

    private readonly List<SkillUpgradeConfig> _skillUpgrades;
    private readonly List<UpgradeState> _upgradeConfigs = new ();

    public UpgradeGenerator(
        List<NewWeaponUpgradeConfig> weapons,
        List<WeaponUpgradeConfig> weaponUpgrades,
        List<NewTurretUpgradeConfig> turrets,
        List<TurretUpgradeConfig> turretUpgrades,
        List<SkillUpgradeConfig> skillUpgrades)
    {
        _weapons = weapons;
        _weaponUpgrades = weaponUpgrades;
        _turrets = turrets;
        _turretUpgrades = turretUpgrades;
        _skillUpgrades = skillUpgrades;
    }

    public List<UpgradeState> GenerateSelection(PlayerInventoryComponent inventory, int count = 3)
    {
        _upgradeConfigs.Clear();

        GetSkillUpgrades(inventory);
        GetWeaponUpgrades(inventory);

        if (_upgradeConfigs.Count > 0)
            return _upgradeConfigs.OrderBy(x => Random.value).Take(count).ToList();

        return null;
    }

    private void GetSkillUpgrades(PlayerInventoryComponent inventory)
    {
        foreach (var upgrade in _skillUpgrades)
        {
            bool hasUpgrade = inventory.ActiveUpgrades.TryGetValue(upgrade.UpgradeId, out int currentLevel);

            if (hasUpgrade && currentLevel >= upgrade.MaxLevel)
                continue;

            if (upgrade.RequiredUpgradeIds != null && upgrade.RequiredUpgradeIds.Count > 0)
            {
                if (upgrade.RequiredUpgradeIds.All(config => inventory.ActiveUpgrades.ContainsKey(config.UpgradeId)) == false)
                    continue;
            }

            if (currentLevel == 0 && inventory.CurrentSkillsAmount == inventory.MaxSkills)
                continue;

            _upgradeConfigs.Add(new UpgradeState { Config = upgrade, CurrentLevel = currentLevel });
        }
    }

    private void GetWeaponUpgrades(PlayerInventoryComponent inventory)
    {
        AddUpgrades(_weapons, inventory);
        AddUpgrades(_weaponUpgrades, inventory);
        AddUpgrades(_turrets, inventory);
        AddUpgrades(_turretUpgrades, inventory);
    }

    private void AddUpgrades<T>(List<T> upgrades, PlayerInventoryComponent inventory)
        where T : UpgradeConfig
    {
        foreach (var upgrade in upgrades)
        {
            bool hasUpgrade = inventory.ActiveUpgrades.TryGetValue(upgrade.UpgradeId, out int currentLevel);

            if (hasUpgrade)
                continue;

            if (upgrade.RequiredUpgradeIds != null && upgrade.RequiredUpgradeIds.Count > 0)
            {
                if (upgrade.RequiredUpgradeIds.All(config => inventory.ActiveUpgrades.ContainsKey(config.UpgradeId)) == false)
                    continue;
            }

            if (currentLevel == 0 && inventory.CurrentWeaponsAmount == inventory.MaxWeapons)
                continue;

            _upgradeConfigs.Add(new UpgradeState { Config = upgrade, CurrentLevel = currentLevel });
        }
    }
}
