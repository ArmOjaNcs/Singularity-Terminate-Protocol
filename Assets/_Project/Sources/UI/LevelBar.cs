using ECS.PlayerSystems;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class LevelBar : MonoBehaviour
{
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TextMeshProUGUI levelText;

    private CompositeDisposable _disposables = new CompositeDisposable();

    public void Setup(PlayerExperienceComponent xpComponent)
    {
        _disposables.Clear();

        xpSlider.minValue = xpComponent.CurrentXP.Value;
        xpSlider.maxValue = xpComponent.NextLevelXP.Value;

        xpComponent.CurrentXP
            .Subscribe(currentXp =>
            {
                xpSlider.value = currentXp;
            })
            .AddTo(_disposables);

        xpComponent.NextLevelXP
            .Subscribe(currentXp =>
            {
                xpSlider.minValue = 0;
                xpSlider.maxValue = xpComponent.NextLevelXP.Value;
            })
            .AddTo(_disposables);

        xpComponent.CurrentLevel
            .Subscribe(newLevel =>
            {
                levelText.text = newLevel.ToString();
            })
            .AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
