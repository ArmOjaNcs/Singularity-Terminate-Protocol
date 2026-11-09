using Scellecs.Morpeh;
using TMPro;
using UniRx;
using UnityEngine;

public class TimerPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;

    public void Setup(ref BattleTimerComponent timer)
    {
        timer.CurrentTime
            .Select(FormatTime)
            .DistinctUntilChanged()
            .Subscribe(text => _timerText.text = text)
            .AddTo(this);
    }

    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
