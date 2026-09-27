using UnityEngine;

namespace Gameplay.Common
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer Renderer;

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void SetFacingLeft(bool value)
        {
            Renderer.flipX = value;
        }
    }
}