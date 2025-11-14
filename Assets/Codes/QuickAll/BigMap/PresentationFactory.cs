using Map.Entity;
using My.Map;
using My.Map.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 工厂类
/// </summary>
public interface IPresentationFactory
{
    IScenePresentation Spawn(ILogicEntity logic, Transform parent);
    void Recycle(IScenePresentation presentation);
}


public interface IPresentationFactoryAsync
{
    Task<IScenePresentation> SpawnAsync(ILogicEntity logic);
    Task RecycleAsync(IScenePresentation presentation);
}



public static class PresentationConfig
{
    public static string GetPrefabKey(EEntityType type, string cfgId)
    {
        // 映射逻辑类型到表现Prefab地址（Addressables key/Resources路径）
        switch (type)
        {
            case EEntityType.InteractPoint: return $"Prefab/Presentations/InteractPoint/{cfgId}";
            case EEntityType.LootPoint: return $"Prefab/Presentations/LootPoint/{cfgId}";
            case EEntityType.Npc: return $"Prefab/Presentations/Npc/{cfgId}";
            case EEntityType.Monster: return $"Prefab/Presentations/Monster/{cfgId}";
            case EEntityType.AreaEffect: return $"Prefab/Presentations/AreaEffect/{cfgId}";
            case EEntityType.DestroyObj:
                return $"Prefab/Presentations/DestroyObj/{cfgId}";
            case EEntityType.AttractPoint:
                return $"Prefab/Presentations/AttractPoint/{cfgId}";
            case EEntityType.PatrolGroup:
                return $"Prefab/Presentations/PatrolGroup";

            // ...
            default: return "Prefab/Presentations/Default";
        }
    }

    public static string ResolveKey(GameObject go)
    {
        // 可通过标记/组件反查对应key，或在 Spawn 时存入字典
        return go.name;
    }
}

public interface IAssetProvider
{
    GameObject Instantiate(string key);
    void Release(GameObject go);
    // 异步版本（推荐生产使用）：Task<GameObject> InstantiateAsync(string key); Task ReleaseAsync(GameObject go);
}

public interface IAssetProviderAsync
{
    Task<GameObject> InstantiateAsync(string key);
    Task ReleaseAsync(GameObject go);
}



public class PresentationFactory : MonoBehaviour, IPresentationFactory, IPresentationFactoryAsync
{
    [SerializeField] private MonoBehaviour assetProviderSource;
    private IAssetProviderAsync _assetAsync;
    private IAssetProvider _asset;
    private readonly Dictionary<string, Stack<GameObject>> _pool = new();

    private void Awake()
    {
        _asset = assetProviderSource as IAssetProvider;
        _assetAsync = assetProviderSource as IAssetProviderAsync;
    }

    public IScenePresentation Spawn(ILogicEntity logic, Transform parent)
    {
        var prefabKey = PresentationConfig.GetPrefabKey(logic.Type, logic.CfgId);
        if(string.IsNullOrEmpty(prefabKey))
        {
            return null;
        }
        var go = TryGet(prefabKey) ?? _asset.Instantiate(prefabKey);
        go.transform.SetParent(parent);
        var pres = go.GetComponent<IScenePresentation>();
        //if (pres == null)
            //pres = go.AddComponent<DefaultPresenter>(); // 兜底
        return pres;
    }

    public void Recycle(IScenePresentation presentation)
    {
        if(presentation == null)
        {
            return;
        }
        var go = (presentation as Component).gameObject;
        var prefabKey = PresentationConfig.ResolveKey(go);
        go.SetActive(false);
        if (!_pool.TryGetValue(prefabKey, out var stack))
        {
            stack = new Stack<GameObject>();
            _pool[prefabKey] = stack;
        }
        stack.Push(go);
    }

    public async Task<IScenePresentation> SpawnAsync(ILogicEntity logic)
    {
        string key = PresentationConfig.GetPrefabKey(logic.Type, logic.CfgId);
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }
        GameObject go = await _assetAsync.InstantiateAsync(key);

        var pres = go.GetComponent<IScenePresentation>();
        if (pres == null)
            pres = go.AddComponent<DefaultPresenter>();
        // 不在这里 Bind/SetVisible，由 AOIManager 完成，以处理取消逻辑
        var dynamicRoot = MainGameManager.Instance.GetDynamicRoot();
        if (dynamicRoot != null)
        {
            go.transform.SetParent(dynamicRoot);
        }
        return pres;
    }

    public Task RecycleAsync(IScenePresentation presentation)
    {
        if (presentation == null)
        {
            return Task.CompletedTask;
        }

        var go = (presentation as Component)?.gameObject;
        if (go != null)
            return _assetAsync.ReleaseAsync(go);
        return Task.CompletedTask;
    }

    private GameObject TryGet(string key)
    {
        if (_pool.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            var go = stack.Pop();
            go.SetActive(true);
            return go;
        }
        return null;
    }
}

// 兜底 Presenter
public class DefaultPresenter : ScenePresentationBase<LogicEntityBase> { }