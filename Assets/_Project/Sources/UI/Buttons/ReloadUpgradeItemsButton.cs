using Core.Services;
using UnityEngine;
using Zenject;

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