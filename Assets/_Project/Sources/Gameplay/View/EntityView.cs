using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.View
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer Renderer;

        public Entity Entity { get; private set; }

        public void SetEntity(Entity entity) => Entity = entity;

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void SetFacingLeft(bool value)
        {
            Renderer.flipX = value;
        }

        public void SetSortingOrder(int sortingOrder)
        {
            Renderer.sortingOrder = sortingOrder;
        }
    }
}