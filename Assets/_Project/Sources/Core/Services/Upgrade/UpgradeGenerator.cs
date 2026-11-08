using System.Collections.Generic;
using System.Linq;
using ECS.PlayerComponents;
using UI;
using UniRx;
using Unity.Collections;
using UnityEngine;
using UpgradeConfigs;

public class UpgradeGenerator
{
    private const int ItemsAmount = 3;
    private const int LuckyItemsAmount = 4;
    private readonly List<NewWeaponUpgradeConfig> _weapons;
    private readonly List<WeaponStatUpgradeConfig> _weaponUpgrades;

    private readonly List<NewTurretUpgradeConfig> _turrets;
    private readonly List<TurretStatUpgradeConfig> _turretUpgrades;

    private readonly List<SkillUpgradeConfig> _skillUpgrades;
    private readonly List<UpgradeState> _upgradeConfigs = new ();

    public UpgradeGenerator(
        List<NewWeaponUpgradeConfig> weapons,
        List<WeaponStatUpgradeConfig> weaponUpgrades,
        List<NewTurretUpgradeConfig> turrets,
        List<TurretStatUpgradeConfig> turretUpgrades,
        List<SkillUpgradeConfig> skillUpgrades)
    {
        _weapons = weapons;
        _weaponUpgrades = weaponUpgrades;
        _turrets = turrets;
        _turretUpgrades = turretUpgrades;
        _skillUpgrades = skillUpgrades;
    }

    public List<UpgradeState> GenerateSelection(PlayerInventoryComponent inventory, bool isLuckyStrike)
    {
        _upgradeConfigs.Clear();

        GetSkillUpgrades(inventory);
        GetNewWeaponUpgrades(inventory);
        GetWeaponUpgrades(inventory);

        int count = isLuckyStrike ? LuckyItemsAmount : ItemsAmount;

        if (_upgradeConfigs.Count > 0)
            return _upgradeConfigs.OrderBy(x => Random.value).Take(count).ToList();

        return null;
    }

    private void GetSkillUpgrades(PlayerInventoryComponent inventory)
    {
        foreach (var upgrade in _skillUpgrades)
        {
            bool hasUpgrade = inventory.ActiveSkills.TryGetValue(upgrade.UpgradeId, out int currentLevel);
            bool isNewSkill = currentLevel == 0;

            if (hasUpgrade && currentLevel >= upgrade.MaxLevel)
                continue;

            if (upgrade.RequiredUpgradeIds != null && upgrade.RequiredUpgradeIds.Count > 0)
            {
                if (upgrade.RequiredUpgradeIds.All(config => inventory.ActiveSkills.ContainsKey(config.UpgradeId)) == false)
                    continue;
            }

            if (hasUpgrade == false && inventory.CurrentItemsAmount == inventory.MaxSkills.Value)
                continue;

            UpgradeState upgradeState = new UpgradeState
            {
                Title = upgrade.Title,
                Icon = upgrade.Icon,
                Description = upgrade.Description,
                CurrentLevel = currentLevel,
                Config = upgrade,
            };

            _upgradeConfigs.Add(upgradeState);
        }
    }

    private void GetNewWeaponUpgrades(PlayerInventoryComponent inventory)
    {
        AddNewWeaponUpgrades(_weapons, inventory);
        AddNewWeaponUpgrades(_turrets, inventory);
    }

    private void GetWeaponUpgrades(PlayerInventoryComponent inventory)
    {
        AddWeaponUpgrades(_weaponUpgrades, inventory);
        AddWeaponUpgrades(_turretUpgrades, inventory);
    }

    private void AddWeaponUpgrades<T>(List<T> upgrades, PlayerInventoryComponent inventory)
        where T : UpgradeConfig
    {
        ReactiveDictionary<string, int> activeWeapons = upgrades is List<WeaponStatUpgradeConfig>
            ? inventory.ActiveWeapons
            : inventory.ActiveTurrets;

        foreach (var weapon in activeWeapons)
        {
            var currentWeapon = _weapons.FirstOrDefault(weaponUpgrade => weaponUpgrade.UpgradeId == weapon.Key);
            var currentWeaponLevel = weapon.Value;
            int maxLevel = currentWeapon.MaxLevel;

            if (currentWeaponLevel >= maxLevel)
                continue;

            var upgrade = upgrades[UnityEngine.Random.Range(0, upgrades.Count)];

            UpgradeState upgradeState = new UpgradeState
            {
                Title = currentWeapon.Title,
                Icon = currentWeapon.Icon,
                Description = upgrade.Description,
                CurrentLevel = currentWeaponLevel,
                Config = upgrade,
                BaseWeaponId = currentWeapon.UpgradeId,
                BaseWeaponType = currentWeapon.Type,
            };

            _upgradeConfigs.Add(upgradeState);
        }
    }

    private void AddNewWeaponUpgrades<T>(List<T> upgrades, PlayerInventoryComponent inventory)
        where T : UpgradeConfig
    {
        ReactiveDictionary<string, int> activeDictionary = new ReactiveDictionary<string, int>();
        int currentWeaponsAmount = 0;
        int maxWeaponsAmount = 0;

        if (upgrades is List<NewWeaponUpgradeConfig>)
        {
            activeDictionary = inventory.ActiveWeapons;
            currentWeaponsAmount = inventory.CurrentWeaponsAmount;
            maxWeaponsAmount = inventory.MaxWeapons.Value;
        }

        if (upgrades is List<NewTurretUpgradeConfig>)
        {
            activeDictionary = inventory.ActiveTurrets;
            currentWeaponsAmount = inventory.CurrentTurretsAmount;
            maxWeaponsAmount = inventory.MaxTurrets.Value;
        }

        foreach (var upgrade in upgrades)
        {
            if (upgrade == null)
                continue;

            bool hasUpgrade = activeDictionary.TryGetValue(upgrade.UpgradeId, out int currentLevel);
            bool hasMaxWeapons = currentLevel == 0 && currentWeaponsAmount == maxWeaponsAmount;

            if (hasUpgrade || hasMaxWeapons)
                continue;

            if (upgrade.RequiredUpgradeIds != null && upgrade.RequiredUpgradeIds.Count > 0)
            {
                if (upgrade.RequiredUpgradeIds.All(config => inventory.ActiveSkills.ContainsKey(config.UpgradeId)) == false)
                    continue;
            }

            UpgradeState upgradeState = new UpgradeState
            {
                Title = upgrade.Title,
                Icon = upgrade.Icon,
                Description = upgrade.Description,
                CurrentLevel = currentLevel,
                Config = upgrade,
            };

            _upgradeConfigs.Add(upgradeState);
        }
    }
}
