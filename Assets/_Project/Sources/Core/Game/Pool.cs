using System.Collections.Generic;
using UnityEngine;

namespace Core.Game
{
    public class ObjectPool<T>
        where T : MonoBehaviour
    {
        private List<T> _pool;
        private T _prefab;

        public ObjectPool(T prefab, int maxCapacity, Transform container)
        {
            _prefab = prefab;
            Container = container;
            CreatePool(maxCapacity);
        }

        public Transform Container { get; }

        public bool TryGetFreeElement(out T element)
        {
            foreach (T freeElement in _pool)
            {
                if (freeElement.gameObject.activeInHierarchy == false)
                {
                    element = freeElement;
                    return true;
                }
            }

            element = null;
            return false;
        }

        public T GetElement()
        {
            if (TryGetFreeElement(out T element))
                return element;

            return CreateObject();
        }

        private T CreateObject()
        {
            var createdObject = GameObject.Instantiate(_prefab);
            _pool.Add(createdObject);
            createdObject.gameObject.SetActive(false);

            return createdObject;
        }

        private void CreatePool(int maxCapacity)
        {
            _pool = new List<T>();

            for (int i = 0; i < maxCapacity; i++)
                CreateObject();
        }
    }
}