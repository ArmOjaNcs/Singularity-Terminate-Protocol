using UnityEngine;
using UnityEngine.UI;
using UpgradeConfigs;

namespace UI
{
    public class EquipIcon : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Sprite _emptyView;

        private bool _isEmpty = true;

        public bool IsEmpty => _isEmpty;

        public void Setup()
        {
            _isEmpty = true;
            _icon.sprite = _emptyView;
        }

        public void FillIcon(Sprite sprite)
        {
            _isEmpty = false;
            _icon.sprite = sprite;
        }
    }
}