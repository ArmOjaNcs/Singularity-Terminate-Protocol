using UnityEngine;

namespace Gameplay
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class ArenaBounds : MonoBehaviour
    {
        private PolygonCollider2D _boundsCollider;
        public Bounds WorldBounds => _boundsCollider.bounds;

        private void Awake()
        {
            _boundsCollider = GetComponent<PolygonCollider2D>();
        }
    }
}