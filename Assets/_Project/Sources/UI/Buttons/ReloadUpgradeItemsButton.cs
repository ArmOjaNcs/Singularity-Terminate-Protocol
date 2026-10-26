using UnityEngine;

namespace UI
{
    public class ReloadUpgradeItemsButton : UIButton
    {
        [SerializeField] private LevelUpScreen _screen;

        public override void HandleClick()
        {
            _screen.UpdateItems();
        }
    }
}