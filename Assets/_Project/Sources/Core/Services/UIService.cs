using System;
using System.Collections.Generic;
using ECS.PlayerComponents;
using UI;
using UnityEngine;
using UpgradeConfigs;
using Zenject;

namespace Core.Services
{
    public class UIService
    {
        private readonly Dictionary<Component, GameObject> _cachedWindows = new ();

        private DiContainer _container;
        private LevelUpScreen _levelUpScreenPrefab;
        private Transform _levelUpContainer;
        private LevelUpScreen _levelUpScreen;

        public UIService(
            Transform levelUpContainer,
            LevelUpScreen levelUpScreenPrefab,
            DiContainer container)
        {
            _levelUpScreenPrefab = levelUpScreenPrefab;
            _levelUpContainer = levelUpContainer;
            _container = container;
        }

        public bool IsLevelUpScreenOpen { get; private set; }

        public void ShowLevelUpScreen(PlayerInventoryComponent inventoryComponent)
        {
            if (_levelUpScreen == null)
            {
                GameObject levelUp = GetOrCreateWindow(_levelUpScreenPrefab, _levelUpContainer);

                _levelUpScreen = levelUp.GetComponent<LevelUpScreen>();
            }

            _levelUpScreen.Setup(inventoryComponent);

            IsLevelUpScreenOpen = true;
        }

        public void HideLevelUpScreen()
        {
            IsLevelUpScreenOpen = false;
            _levelUpScreen.Close();
        }

        private GameObject GetOrCreateWindow(UIScreen prefab, Transform container)
        {
            if (_cachedWindows.TryGetValue(prefab, out GameObject activeWindow))
                return activeWindow;

            GameObject spawnedInstance = _container.InstantiatePrefab(prefab, container);

            _cachedWindows[prefab] = spawnedInstance;

            return spawnedInstance;
        }
    }
}