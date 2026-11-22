using UnityEngine;

namespace Gameplay.View
{
    public sealed class EmpWaveView : AnimatedView, IAreaExpansionView
    {
        private float _radius;

        public void Begin(float radius)
        {
            _radius = radius;
            SetExpansionProgress(0f);
        }

        public void SetExpansionProgress(float progress)
        {
            float spriteRadius = Renderer.sprite.bounds.extents.x;
            Renderer.transform.localScale = Vector3.one * (_radius * Mathf.Clamp01(progress) / spriteRadius);
        }
    }
}
