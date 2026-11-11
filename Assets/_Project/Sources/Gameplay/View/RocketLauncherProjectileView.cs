using UnityEngine;

namespace Gameplay.View
{
    public sealed class RocketLauncherProjectileView : AnimatedView, IAreaExpansionView
    {
        [SerializeField] private SpriteRenderer _projectileRenderer;
        [SerializeField] private SpriteRenderer _explosionRenderer;
        [SerializeField] private Transform _explosionTransform;
        [SerializeField] private Vector3 _explosionFullScale = Vector3.one * 0.36f;

        public void BeginFlight()
        {
            if (_projectileRenderer != null)
                _projectileRenderer.enabled = true;

            if (_explosionRenderer != null)
                _explosionRenderer.enabled = false;

            if (_explosionTransform != null)
                _explosionTransform.localScale = Vector3.zero;
        }

        public void BeginExplosion()
        {
            if (_projectileRenderer != null)
                _projectileRenderer.enabled = false;

            if (_explosionRenderer != null)
                _explosionRenderer.enabled = true;

            SetExpansionProgress(0f);
        }

        public void SetExpansionProgress(float progress)
        {
            if (_explosionTransform != null)
            {
                _explosionTransform.localScale =
                    _explosionFullScale * Mathf.Clamp01(progress);
            }
        }

        public override void SetSortingOrder(int sortingOrder)
        {
            base.SetSortingOrder(sortingOrder);
            if (_explosionRenderer != null)
                _explosionRenderer.sortingOrder = sortingOrder;
        }
    }
}
