using Core.Services;
using Zenject;

namespace UI
{
    public class LevelUpScreen : UIScreen
    {
        public override void Setup()
        {
            gameObject.SetActive(true);
        }

        public override void Close()
        {
            gameObject.SetActive(false);
        }
    }
}