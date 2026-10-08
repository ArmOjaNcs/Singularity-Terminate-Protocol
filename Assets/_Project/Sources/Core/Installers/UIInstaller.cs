using Core.Services;
using UI;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    public class UIInstaller : MonoInstaller
    {
        [Header("Objects")]
        [SerializeField] private LevelBar _levelBar;

        [Header("Prefabs")]
        [SerializeField] private LevelUpScreen _levelUpScreenPrefab;

        [Header("Containers")]
        [SerializeField] private Transform _levelUpContainer;

        public override void InstallBindings()
        {
            BindInstances();

            Container.Bind<UIService>().AsSingle().WithArguments(_levelUpScreenPrefab, _levelUpContainer);
        }

        private void BindInstances()
        {
            Container.BindInstance(_levelBar).AsSingle();
        }
    }
}