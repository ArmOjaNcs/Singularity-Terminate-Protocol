using Core.Services;
using UnityEngine;
using Zenject;

namespace Core.Installers
{
    public class GlobalInstaller : MonoInstaller
    {
        [SerializeField] private AudioService _audioService;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<AudioService>()
                .FromComponentInNewPrefab(_audioService)
                .UnderTransformGroup("GlobalServices")
                .AsSingle()
                .NonLazy();
        }
    }
}
