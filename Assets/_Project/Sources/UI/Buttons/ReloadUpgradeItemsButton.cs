using UnityEngine;

namespace UI
{
    public class ReloadUpgradeItemsButton : UIButton
    {
        [SerializeField] private GameObject _screen;

        public override void HandleClick()
        {
            _screen.SetActive(false);
        }
    }
}