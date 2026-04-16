using UnityEngine;
using System.Collections.Generic;

namespace My.Map.View
{
    public class TextBubblePool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        public TextBubblePool(GameObject prefab, int size, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < size; i++)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }

        public GameObject Get()
        {
            if (_pool.Count > 0)
            {
                var go = _pool.Dequeue();
                go.SetActive(true);
                return go;
            }
            else
            {
                // 池满时可按需扩容
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(true);
                return go;
            }
        }

        public void Release(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(_parent);
            _pool.Enqueue(go);
        }
    }
}

