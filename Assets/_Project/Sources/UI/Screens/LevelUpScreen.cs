using System;
using System.Collections.Generic;
using DG.Tweening;
using ECS.PlayerComponents;
using UnityEngine;
using Zenject;

namespace UI
{
    public class LevelUpScreen : UIScreen
    {
        [Inject] private UpgradeGenerator _generator;
        [Inject] private DiContainer _container;

        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private UpgradeItem _upgradeItem;
        [SerializeField] private GameObject _luckInformationItem;

        [Header("Настройки анимации")]
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private Ease _easeType = Ease.OutBack;
        [SerializeField] private float _delayBetweenCards = 0.1f;
        [SerializeField] private float _scaleFactor = 0.9f;

        private Vector2 _centerPosition = Vector2.zero;
        private Tween _activeTween;

        private Dictionary<UpgradeItem, UpgradeState> _items = new Dictionary<UpgradeItem, UpgradeState>();
        private PlayerInventoryComponent _inventory;
        private bool _isLuckyStrike;

        public void Setup(PlayerInventoryComponent inventory, bool isLuckyStrike)
        {
            _inventory = inventory;
            _isLuckyStrike = isLuckyStrike;

            UpdateItems();
            PerformSetupAnimation();
        }

        public void UpdateForAds()
        {
            UpdateItems();
            AnimateItems();
        }

        public override void Close()
        {
            ClearScreen();
            _activeTween?.Kill();
            gameObject.SetActive(false);
        }

        private void UpdateItems()
        {
            ClearScreen();

            gameObject.SetActive(true);

            var states = _generator.GenerateSelection(_inventory, _isLuckyStrike);

            foreach (var state in states)
            {
                var card = _container.InstantiatePrefabForComponent<UpgradeItem>(_upgradeItem, _cardsContainer);

                _items.Add(card, state);
                card.Setup(state);
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

        private void ClearScreen()
        {
            _items.Clear();
            _luckInformationItem.SetActive(false);

            foreach (Transform child in _cardsContainer)
            {
                if (child.GetComponent<UpgradeItem>())
                    Destroy(child.gameObject);
            }
        }

        private void PerformSetupAnimation()
        {
            _activeTween?.Kill();

            float startX = Screen.width + (_rectTransform.rect.width / 2f);

            _rectTransform.anchoredPosition = new Vector2(startX, _centerPosition.y);
            _rectTransform.gameObject.SetActive(true);

            _activeTween = _rectTransform.DOAnchorPos(_centerPosition, _duration)
                .SetEase(_easeType)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    AnimateItems();
                });
        }

        private void AnimateItems()
        {
            float currentDelay = 0f;

            foreach (var item in _items)
            {
                item.Key.Animate(currentDelay);
                currentDelay += _delayBetweenCards;
            }

            _luckInformationItem.transform.localScale = Vector3.one * _scaleFactor;
            _luckInformationItem.transform.DOScale(Vector3.one, _duration)
                .SetEase(Ease.OutQuad)
                .SetDelay(currentDelay)
                .SetUpdate(true);
        }
    }
}
