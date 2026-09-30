using Core.Services;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    public class GlobalInstaller : MonoInstaller
    {
        [SerializeField] private AudioService _audioService;
        [SerializeField] private UIService _uiService;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<AudioService>()
                .FromComponentInNewPrefab(_audioService)
                .UnderTransformGroup("GlobalServices")
                .AsSingle()
                .NonLazy();

            Container.BindInterfacesAndSelfTo<UIService>()
                .FromComponentInNewPrefab(_uiService)
                .UnderTransformGroup("GlobalServices")
                .AsSingle()
                .NonLazy();
        }
    }
}

