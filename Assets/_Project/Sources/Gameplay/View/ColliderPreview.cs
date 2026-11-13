using UnityEngine;

namespace Gameplay.View
{
    public sealed class ColliderPreview : MonoBehaviour
    {
        [SerializeField] private ColliderEntityConfig _config;
        [SerializeField] private bool _showHitBox = true;
        [SerializeField] private bool _showNavigationBox = true;

        public ColliderEntityConfig Config => _config;

        public bool ShowHitBox => _showHitBox;

        public bool ShowNavigationBox => _showNavigationBox;

        private void OnDrawGizmos()
        {
            if (_config == null)
                return;

            if (_showHitBox)
                ColliderPreviewGizmos.Draw(_config.HitBox, transform, Color.cyan);

            if (_showNavigationBox)
            {
                ColliderPreviewGizmos.Draw(
                    _config.NavigationBox ?? _config.HitBox,
                    transform,
                    Color.green);
            }
        }
    }
}
