using Core.Services;
using Zenject;

namespace UI
{
    public class ChooseUpgradeItemButton : UIButton
    {
        [Inject] private UIService _uiService;

        public override void HandleClick()
        {
            _uiService.HideLevelUpScreen();
        }
    }
}