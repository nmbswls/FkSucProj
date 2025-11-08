using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleResourcesProvider : MonoBehaviour, IAssetProvider, IAssetProviderAsync
{
    public GameObject Instantiate(string key)
    {
        var prefab = Resources.Load<GameObject>(key);
        return GameObject.Instantiate(prefab);
    }

    public void Release(GameObject go)
    {
        GameObject.Destroy(go);
    }

    public async Task<GameObject> InstantiateAsync(string key)
    {
        ResourceRequest req = Resources.LoadAsync<GameObject>(key); // === 新增 ===

        while (!req.isDone) // === 新增 ===
        {
            await Task.Yield(); 
        }

        var prefab = req.asset as GameObject; // === 新增 ===
        if (prefab == null) // === 新增 ===
        {
            Debug.LogError($"SimpleResourcesProviderAsync: LoadAsync failed, key={key}"); // === 新增 ===
            return null; // === 新增 ===
        }

        return GameObject.Instantiate(prefab); // === 新增 ===
    }

    public Task ReleaseAsync(GameObject go)
    {
        Release(go);
        return Task.CompletedTask;
    }
}
