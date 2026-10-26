using System.Collections.Generic;
using ECS.PlayerComponents;
using UnityEngine;
using Zenject;

namespace UI
{
    public class LevelUpScreen : UIScreen
    {
        [Inject] private UpgradeGenerator _generator;
        [Inject] private DiContainer _container;

        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private UpgradeItem _upgradeItem;
        [SerializeField] private GameObject _luckInformationItem;

        private List<UpgradeState> _items;
        private PlayerInventoryComponent _inventory;
        private bool _isLuckyStrike;

        public void Setup(PlayerInventoryComponent inventory, bool isLuckyStrike)
        {
            _inventory = inventory;
            _isLuckyStrike = isLuckyStrike;

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

            _items = _generator.GenerateSelection(_inventory, _isLuckyStrike);

            foreach (var item in _items)
            {
                var card = _container.InstantiatePrefabForComponent<UpgradeItem>(_upgradeItem, _cardsContainer);
                card.Setup(item);
            }

            if (_isLuckyStrike)
            {
                _luckInformationItem.SetActive(false);
            }
            else
            {
                _luckInformationItem.SetActive(true);
                _luckInformationItem.transform.SetAsLastSibling();
            }
        }

        public override void Close()
        {
            gameObject.SetActive(false);
        }
    }
}