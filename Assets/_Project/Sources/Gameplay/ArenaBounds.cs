using UnityEngine;

namespace Gameplay
{
    public sealed class ArenaBounds : MonoBehaviour
    {
        [SerializeField] private BoxCollider _boundsCollider;

        public Bounds WorldBounds => _boundsCollider.bounds;

        private void Reset()
        {
            _boundsCollider = GetComponent<BoxCollider>();
        }

        private void Awake()
        {
            if (_boundsCollider == null)
            {
                _boundsCollider = GetComponent<BoxCollider>();
            }
        }
    }
}