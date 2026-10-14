using System.Collections.Generic;
using Core.Services;
using ECS.PlayerComponents;
using UnityEngine;
using UpgradeConfigs;
using Zenject;
using static UnityEditor.Progress;

namespace UI
{
    public class LevelUpScreen : UIScreen
    {
        private const int LuckyItemsAmount = 4;
        [Inject] private UpgradeGenerator _generator;
        [Inject] private DiContainer _container;

        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private UpgradeItem _upgradeItem;
        [SerializeField] private GameObject _luckInformationItem;

        private List<UpgradeState> _items;
        private PlayerInventoryComponent _inventory;

        public void Setup(PlayerInventoryComponent inventory)
        {
            _inventory = inventory;

            UpdateItems();
        }

        public void UpdateItems()
        {
            foreach (Transform child in _cardsContainer)
            {
                if (child.GetComponent<UpgradeItem>())
                    Destroy(child.gameObject);
            }

            gameObject.SetActive(true);

            _items = _generator.GenerateSelection(_inventory);

            foreach (var item in _items)
            {
                var card = _container.InstantiatePrefabForComponent<UpgradeItem>(_upgradeItem, _cardsContainer);
                card.Setup(item);
            }

            _luckInformationItem.SetActive(_items.Count < LuckyItemsAmount);
            _luckInformationItem.transform.SetAsLastSibling();
        }

        public override void Close()
        {
            gameObject.SetActive(false);
        }
    }
}