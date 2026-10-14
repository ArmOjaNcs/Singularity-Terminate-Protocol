using System.Collections.Generic;
using System.Linq;
using ECS.PlayerComponents;
using UI;
using UnityEngine;
using UpgradeConfigs;

public class UpgradeGenerator
{
    private readonly List<WeaponUpgradeConfig> _weaponUpgrades;
    private readonly List<TurretUpgradeConfig> _turretUpgrades;
    private readonly List<SkillUpgradeConfig> _skillUpgrades;
    private readonly List<UpgradeState> _upgradeConfigs = new ();

    public UpgradeGenerator(
        List<WeaponUpgradeConfig> weaponUpgrades,
        List<TurretUpgradeConfig> turretUpgrades,
        List<SkillUpgradeConfig> skillUpgrades)
    {
        _weaponUpgrades = weaponUpgrades;
        _turretUpgrades = turretUpgrades;
        _skillUpgrades = skillUpgrades;
    }

    public List<UpgradeState> GenerateSelection(PlayerInventoryComponent inventory, int count = 3)
    {
        _upgradeConfigs.Clear();

        GetSkillUpgrades(inventory);
        GetWeaponUpgrades(inventory);
        GetTurretUpgrades(inventory);

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
        foreach (var upgrade in _weaponUpgrades)
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

    private void GetTurretUpgrades(PlayerInventoryComponent inventory)
    {
        foreach (var upgrade in _turretUpgrades)
        {
            bool hasUpgrade = inventory.ActiveUpgrades.TryGetValue(upgrade.UpgradeId, out int currentLevel);

            if (hasUpgrade)
                continue;

            if (upgrade.RequiredUpgradeIds != null && upgrade.RequiredUpgradeIds.Count > 0)
            {
                if (upgrade.RequiredUpgradeIds.All(config => inventory.ActiveUpgrades.ContainsKey(config.UpgradeId)) == false)
                    continue;
            }

            if (currentLevel == 0 && inventory.CurrentTurretsAmount == inventory.MaxTurrets)
                continue;

            _upgradeConfigs.Add(new UpgradeState { Config = upgrade, CurrentLevel = currentLevel });
        }
    }
}
