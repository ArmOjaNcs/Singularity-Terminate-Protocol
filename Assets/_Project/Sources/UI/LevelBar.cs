using ECS.PlayerSystems;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class LevelBar : MonoBehaviour
{
    [SerializeField] private Slider xpSlider;

    private CompositeDisposable _disposables = new CompositeDisposable();

    public void Setup(PlayerExperienceComponent xpComponent)
    {
        _disposables.Clear();

        xpSlider.maxValue = xpComponent.NextLevelXP;

        xpComponent.CurrentXP
            .Subscribe(currentXp =>
            {
                xpSlider.value = currentXp;
            })
            .AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
