using UnityEngine;

namespace Gameplay.Common
{
    public class EntityView : MonoBehaviour
    {
        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}