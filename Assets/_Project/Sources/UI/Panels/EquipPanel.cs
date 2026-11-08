using System;
using ECS.PlayerComponents;
using UniRx;
using UnityEngine;
using UpgradeConfigs;
using Zenject;

namespace UI
{
    public class EquipPanel : MonoBehaviour
    {
        [Inject] private UpgradeStorage _storage;

        [SerializeField] private EquipItemsPanel _weaponsPanel;
        [SerializeField] private EquipItemsPanel _itemsPanel;
        [SerializeField] private EquipItemsPanel _turretsPanel;

        public void Setup(PlayerInventoryComponent inventory)
        {
            InitializePanel(inventory);
            SubscribeOnInventoryChanges(inventory);
        }

        public void UpdateItem(UpgradeConfig upgrade)
        {
            switch (upgrade.UpgradeType)
            {
                case UpgradeType.Item:
                    _itemsPanel.UpdateItem(upgrade);
                    break;

                case UpgradeType.NewWeapon:
                    _weaponsPanel.UpdateItem(upgrade);
                    break;

                case UpgradeType.NewTurret:
                    _turretsPanel.UpdateItem(upgrade);
                    break;

                default:
                    Debug.Log("Неизвестный тип апгрейда!");
                    break;
            }
        }

        private void InitializePanel(PlayerInventoryComponent inventory)
        {
            _weaponsPanel.UpdateCount(inventory.MaxWeapons.Value);
            _turretsPanel.UpdateCount(inventory.MaxTurrets.Value);
            _weaponsPanel.UpdateCount(inventory.MaxSkills.Value);

            foreach (var upgrade in inventory.ActiveWeapons)
            {
                UpgradeConfig config = _storage.GetConfig(upgrade.Key);

                if (config != null)
                    UpdateItem(config);
            }
        }

        private void SubscribeOnInventoryChanges(PlayerInventoryComponent inventory)
        {
            inventory.ActiveSkills.ObserveAdd()
                .Subscribe(upgrade =>
                {
                    UpgradeConfig config = _storage.GetConfig(upgrade.Key);

                    if (config.UpgradeType == UpgradeType.NewWeapon || config.UpgradeType == UpgradeType.NewTurret ||
                       (config.UpgradeType == UpgradeType.Item && upgrade.Value == 1))
                        UpdateItem(config);
                })
                .AddTo(this);

            inventory.MaxWeapons
                .Subscribe(amount => _weaponsPanel.UpdateCount(amount))
                .AddTo(this);

            inventory.MaxTurrets
                .Subscribe(amount => _turretsPanel.UpdateCount(amount))
                .AddTo(this);

            inventory.MaxSkills
                .Subscribe(amount => _itemsPanel.UpdateCount(amount))
                .AddTo(this);
        }
    }
}