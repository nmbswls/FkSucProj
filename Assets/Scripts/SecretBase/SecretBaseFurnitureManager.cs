using UnityEngine;

namespace My.SecretBase
{
    // 首版：运行时摆放，不持久化。
    public class SecretBaseFurnitureManager
    {
        Transform _root;

        public void BindRoot(Transform root)
        {
            _root = root;
        }

        public GameObject SpawnPrefab(GameObject prefab, Vector3 localPos)
        {
            if (prefab == null || _root == null)
            {
                return null;
            }

            var go = Object.Instantiate(prefab, _root);
            go.transform.localPosition = localPos;
            return go;
        }
    }
}
