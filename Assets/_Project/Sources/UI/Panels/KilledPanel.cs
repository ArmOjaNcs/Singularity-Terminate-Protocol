using ECS.CommonComponents;
using ECS.DamageComponents;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace UI
{
    public class KilledPanel : MonoBehaviour
    {
        [Inject] private BattleStatistics _stats;

        [SerializeField] private TextMeshProUGUI _killedText;

        private void OnEnable()
        {
            _stats.TotalKilled
                .Subscribe(killed =>
                {
                    _killedText.text = killed.ToString();
                })
                .AddTo(this);
        }
    }
}