using Configs;
using UnityEngine;

namespace Gameplay.View
{
    public sealed class ColliderPreview : MonoBehaviour
    {
        public CreatureConfig Config;
        public bool ShowHitBox = true;
        public bool ShowNavigationBox = true;

        private void OnDrawGizmos()
        {
            if (Config == null)
                return;

            if (ShowHitBox)
                ColliderPreviewGizmos.Draw(Config.HitBox, transform, Color.cyan);

            if (ShowNavigationBox)
            {
                ColliderPreviewGizmos.Draw(
                    Config.NavigationBox ?? Config.HitBox,
                    transform,
                    Color.green);
            }
        }
    }
}
