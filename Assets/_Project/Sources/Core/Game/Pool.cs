using System.Collections.Generic;
using UnityEngine;

namespace Core.Game
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly List<T> _pool;
        private readonly T _prefab;

        public ObjectPool(T prefab, int maxCapacity, Transform container)
        {
            _prefab = prefab;
            Container = container;

            maxCapacity = Mathf.Max(maxCapacity, CombatSettings.MinPoolCapacity);

            _pool = new List<T>(maxCapacity);

            CreatePool(maxCapacity);
        }

        public Transform Container { get; }

        public bool TryGetFreeElement(out T element)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                T poolElement = _pool[i];

                if (poolElement == null)
                    continue;

                if (!poolElement.gameObject.activeInHierarchy)
                {
                    element = poolElement;
                    return true;
                }
            }

            element = null;
            return false;
        }

        public T GetElement()
        {
            if (_prefab == null)
                return null;

            if (TryGetFreeElement(out T element))
                return element;

            return CreateObject();
        }

        public void Release(T element)
        {
            if (element == null)
                return;

            element.gameObject.SetActive(false);
        }

        private T CreateObject()
        {
            if (_prefab == null)
                return null;

            T createdObject =
                Object.Instantiate(_prefab, Container);

            _pool.Add(createdObject);
            createdObject.gameObject.SetActive(false);

            return createdObject;
        }

        private void CreatePool(int maxCapacity)
        {
            for (int i = 0; i < maxCapacity; i++)
                CreateObject();
        }
    }
}