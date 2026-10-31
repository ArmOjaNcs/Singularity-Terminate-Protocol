using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.View
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer Renderer;
        private Color _baseColor = Color.white;

        public Entity Entity { get; private set; }
        public Color BaseColor => _baseColor;
        public bool IsXFlipped => Renderer != null && Renderer.flipX;

        protected virtual void Awake()
        {
            if (Renderer != null)
                _baseColor = Renderer.color;
        }

        public void SetColor(Color color)
        {
            if (Renderer != null)
                Renderer.color = color;
        }

        public void SetEntity(Entity entity) => Entity = entity;

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void SetFacingLeft(bool value)
        {
            if (Renderer == null || Renderer.flipX == value)
                return;

            Renderer.flipX = value;
        }

        public void ResetFacing()
        {
            SetFacingLeft(false);
        }

        public void SetSortingOrder(int sortingOrder)
        {
            Renderer.sortingOrder = sortingOrder;
        }
    }
}
