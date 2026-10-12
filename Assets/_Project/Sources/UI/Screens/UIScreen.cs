using UnityEngine;
using Zenject;

namespace UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        public virtual void Setup()
        {
        }

        public virtual void Close()
        {
        }

        public class Factory : PlaceholderFactory<Transform, GameObject, UIScreen>
        {
        }
    }
}