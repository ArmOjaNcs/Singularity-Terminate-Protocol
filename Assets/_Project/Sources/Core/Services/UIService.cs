using System.Collections.Generic;
using UI;
using UnityEngine;
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

        public bool IsLevelUpScreenOpen { get; private set; }

        [Inject]
        public void Construct(
            Transform levelUpContainer,
            LevelUpScreen levelUpScreenPrefab,
            DiContainer container)
        {
            _levelUpScreenPrefab = levelUpScreenPrefab;
            _levelUpContainer = levelUpContainer;
            _container = container;
        }

        public void ShowLevelUpScreen()
        {
            GameObject levelUp = GetOrCreateWindow(_levelUpScreenPrefab, _levelUpContainer);

            _levelUpScreen = levelUp.GetComponent<LevelUpScreen>();
            _levelUpScreen.Setup();

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