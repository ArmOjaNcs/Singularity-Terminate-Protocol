using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpgradeConfigs;

public class UpgradeStorage
{
    private readonly Dictionary<string, UpgradeConfig> _upgrades;

    public UpgradeStorage(List<UpgradeConfig> configs)
    {
        _upgrades = configs.ToDictionary(cfg => cfg.UpgradeId, cfg => cfg);
    }

    public UpgradeConfig GetConfig(string upgradeId)
    {
        if (_upgrades.TryGetValue(upgradeId, out var config))
            return config;

        Debug.LogError($"Конфиг для апгрейда {upgradeId} не найден!");
        return null;
    }
}
