using UnityEngine;

namespace Gameplay.View
{
    public interface IAreaExpansionView
    {
        void SetExpansionProgress(float progress);
    }

    public sealed class ExplosiveProjectileView : AnimatedView, IAreaExpansionView
    {
        private static readonly int ExplosionState = Animator.StringToHash("Base Layer.Explosion");

        [SerializeField] private Vector3 _explosionFullScale = Vector3.one * 0.2f;

        private Vector3 _flightScale;

        protected override void Awake()
        {
            base.Awake();
            _flightScale = Renderer.transform.localScale;
        }

        private void OnEnable()
        {
            // The animator enters its default Flight state when the pooled object is activated.
            Renderer.transform.localScale = _flightScale;
        }

        public void BeginExplosion()
        {
            Animator.Play(ExplosionState, 0, 0f);
            SetExpansionProgress(0f);
        }

        public void SetExpansionProgress(float progress)
        {
            Renderer.transform.localScale = _explosionFullScale * Mathf.Clamp01(progress);
        }
    }
}