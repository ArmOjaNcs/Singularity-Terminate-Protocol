using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UpgradeConfigs;

public class EquipItemsPanel : MonoBehaviour
{
    [SerializeField] private EquipIcon _iconPrefab;

    private List<EquipIcon> _items = new List<EquipIcon>();

    public void UpdateCount(int amount)
    {
        if (_items.Count == 0)
        {
            for (int i = 0; i < amount; i++)
            {
                EquipIcon equipIcon = Instantiate(_iconPrefab, transform);

                equipIcon.Setup();
                _items.Add(equipIcon);
            }
        }
    }

    public void UpdateItem(UpgradeConfig config)
    {
        if (_items.Count == 0)
            throw new ArgumentNullException(nameof(_items));

        foreach (var item in _items)
        {
            if (item.IsEmpty)
            {
                item.FillIcon(config.Icon);
                break;
            }
        }
    }
}
