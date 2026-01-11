using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using My.Map.Scene;
using UnityEngine.UI;
using My.Config;
using cfg.demo;

public class WorldAreaManager : MonoBehaviour
{
    public static WorldAreaManager Instance { get; private set; }

    public int currentAreaId;
    public MapAreaInfo? cacheAreaInfo;
    public WorldAreaRoot currentRoot;


    public readonly List<Scene> loadedSubScenes = new List<Scene>();

    public event Action<int> OnWorldLoaded;
    public event Action<int> OnWorldUnloaded;
    public event Action<string, float> OnLoadingProgress; // 子场景名，进度0-1

    public ObstacleSegmentProvider SegmentProvider;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SegmentProvider = GetComponent<ObstacleSegmentProvider>();

        DontDestroyOnLoad(gameObject);
    }

    public bool IsWorldLoaded => currentAreaId != 0;

    public void LoadWorld(int areaId, bool setActive = true, Action<int, bool>? onComplete = null)
    {
        StartCoroutine(CoLoadWorld(areaId, setActive, onComplete));
    }

    public void UnloadCurrentWorld(Action? onUnload)
    {
        if (currentAreaId == 0) return;
        StartCoroutine(CoUnloadWorld(onUnload));
        MainGameManager.Instance.SceneFadeManager.OnLeaveArea();
        currentRoot = null;
    }

    public void Reload()
    {
        if (currentAreaId == 0) return;
        LoadWorld(currentAreaId);
    }

    private IEnumerator CoLoadWorld(int areaId, bool setActive, Action<int, bool>? onComplete)
    {
        // 先卸载旧的
        if (currentAreaId != 0)
            yield return CoUnloadWorld(null);

        loadedSubScenes.Clear();

        var areaCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(areaId);
        if(areaCfg == null)
        {
            Debug.LogError("CoLoadWorld not found.");
            // todo 处理异常情况
            // 保底
            yield break;
        }

        if(string.IsNullOrEmpty(areaCfg.SceneName))
        {
            Debug.LogError("CoLoadWorld area SceneName empty.");
            // todo 处理异常情况
            // 保底
            yield break;
        }

        currentAreaId = areaId;
        cacheAreaInfo = areaCfg;

        do
        {
            if (!IsInBuildSettings(areaCfg.SceneName))
            {
                Debug.LogError($"SubSceneManager: scene '{areaCfg.SceneName}' not in Build Settings.");
                continue;
            }
            var op = SceneManager.LoadSceneAsync(areaCfg.SceneName, LoadSceneMode.Additive);
            if (op == null) { Debug.LogError($"LoadSceneAsync returned null for {areaCfg.SceneName}"); continue; }
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                OnLoadingProgress?.Invoke(areaCfg.SceneName, op.progress);
                yield return null;
            }

            var scene = SceneManager.GetSceneByName(areaCfg.SceneName);
            if (scene.IsValid()) loadedSubScenes.Add(scene);
            else Debug.LogError($"Loaded scene invalid: {areaCfg.SceneName}");
        }
        while (false);

        
        GameObject onlyRoot = null;
        // 设置激活场景（影响 Instantiate 的默认归属、Lighting、NavMesh 等）
        if (setActive)
        {
            var active = loadedSubScenes.FirstOrDefault();
            if (active.IsValid())
            {
                SceneManager.SetActiveScene(active);
                var roots = active.GetRootGameObjects();
                onlyRoot = roots.FirstOrDefault();
                foreach (var root in roots)
                {
                    var comp = root.GetComponent<WorldAreaRoot>();
                    if(comp != null)
                    {
                        currentRoot = comp;
                        break;
                    }
                }
            }
            else
            {
                // 若指定的 activeSubScene未加载，默认设为第一个加载的
                if (loadedSubScenes.Count > 0)
                    SceneManager.SetActiveScene(loadedSubScenes[0]);
            }
        }

        OnWorldLoaded?.Invoke(areaId);
        Debug.Log($"SubSceneManager: World '{areaCfg.SceneName}' loaded with {loadedSubScenes.Count} sub-scenes.");

        SegmentProvider.OnAreaEnter();
        onComplete?.Invoke(currentAreaId, true);
    }

    private IEnumerator CoUnloadWorld(Action? onUnload)
    {
        // 逐个卸载
        for (int i = loadedSubScenes.Count - 1; i >= 0; --i)
        {
            var scene = loadedSubScenes[i];
            if (!scene.IsValid()) continue;

            var op = SceneManager.UnloadSceneAsync(scene);
            while (op != null && !op.isDone)
                yield return null;
        }
        loadedSubScenes.Clear();

        var lastAreaId = currentAreaId;
        currentAreaId = 0;
        OnWorldUnloaded?.Invoke(lastAreaId);
        Debug.Log("SubSceneManager: world unloaded.");

        onUnload?.Invoke();
    }

    private bool IsInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }


    #region 可行走分析


    public Vector2 ClampPathToWalkable(
        Vector2 current,
        Vector2 desired,
        float maxStep = 0.2f) // 每次子步最大长度
    {
        Vector2 pos = current;
        Vector2 totalDelta = desired - current;
        float length = totalDelta.magnitude;
        if (length <= Mathf.Epsilon)
            return current;

        Vector2 dir = totalDelta / length;
        int steps = Mathf.CeilToInt(length / Mathf.Max(maxStep, 1e-4f));
        float stepLen = length / steps;

        for (int i = 0; i < steps; i++)
        {
            Vector2 stepTarget = pos + dir * stepLen;

            if (IsWorldPosWalkable(stepTarget))
            {
                pos = stepTarget;
                continue;
            }

            Vector2 delta = stepTarget - pos;
            Vector2 stepX = new Vector2(delta.x, 0f);
            Vector2 stepY = new Vector2(0f, delta.y);

            if (stepX.sqrMagnitude > 0f && IsWorldPosWalkable(pos + stepX))
            {
                pos = pos + stepX;
                continue;
            }
            if (stepY.sqrMagnitude > 0f && IsWorldPosWalkable(pos + stepY))
            {
                pos = pos + stepY;
                continue;
            }

            // 本子步无法前进，则终止，返回已达到的最近合法点
            break;
        }

        return pos;
    }

    // 世界坐标判定入口
    public bool IsWorldPosWalkable(Vector3 worldPos)
    {
        if (currentRoot == null) return false;

        var cell = currentRoot.Grid.WorldToCell(worldPos);
        return IsCellWalkable(cell);
    }

    private bool IsCellInBounds(Vector3Int cell)
    {
        return currentRoot.TileGround.cellBounds.Contains(cell);
    }

    private bool IsCellBlockedByTile(Vector3Int cell)
    {
        return currentRoot.TileHole != null && currentRoot.TileHole.GetTile(cell) != null;
    }

    private bool IsCellWalkable(Vector3Int cell)
    {
        // 边界外直接不可走
        if (!IsCellInBounds(cell)) return false;
        // 不在行走区域
        if(currentRoot.TileGround.GetTile(cell) == null)
        {
            return false;
        }
        return true;
    }


    #endregion

}