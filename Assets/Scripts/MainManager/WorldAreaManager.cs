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
using My;

public class WorldAreaManager : MonoBehaviour
{
    public static WorldAreaManager Instance 
    { 
        get 
        {
            return MainGameManager.Instance.WorldAreaManager;
        }
    }

    public string currentOverlayMapId;
    public AreaOverlayStateInfo? cacheAreaOverlayInfo;
    public WorldAreaRoot currentRoot;


    public readonly List<Scene> loadedSubScenes = new List<Scene>();

    public event Action<string> OnWorldLoaded;
    public event Action<int> OnWorldUnloaded;
    public event Action<string, float> OnLoadingProgress; // 子场景名，进度0-1

    public ObstacleSegmentProvider SegmentProvider;

    void Awake()
    {
        SegmentProvider = GetComponent<ObstacleSegmentProvider>();
    }

    public bool IsWorldLoaded => !string.IsNullOrEmpty(currentOverlayMapId) && currentRoot != null;

    public void LoadWorld(string mapOverlayId, bool setActive = true, Action<int, bool>? onComplete = null)
    {
        StartCoroutine(CoLoadWorld(mapOverlayId, setActive, onComplete));
    }

    public void UnloadCurrentWorld(Action? onUnload)
    {
        if (string.IsNullOrEmpty(currentOverlayMapId)) 
        {
            onUnload?.Invoke();
            return;
        }
        StartCoroutine(CoUnloadWorld(onUnload));
        MainGameManager.Instance.SceneFadeManager.OnLeaveArea();
        currentRoot?.ClearWalkGrid();
        currentRoot = null;
    }

    public void Reload()
    {
        if (string.IsNullOrEmpty(currentOverlayMapId)) return;
        LoadWorld(currentOverlayMapId);
    }

    private IEnumerator CoLoadWorld(string mapOverlayId, bool setActive, Action<int, bool>? onComplete)
    {
        // 先卸载旧的
        if (!string.IsNullOrEmpty(currentOverlayMapId))
            yield return CoUnloadWorld(null);

        loadedSubScenes.Clear();

        var mapOverlayCfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(mapOverlayId);
        if(mapOverlayCfg == null)
        {
            Debug.LogError("CoLoadWorld not found.");
            // todo 处理异常情况
            // 保底
            yield break;
        }

        if(string.IsNullOrEmpty(mapOverlayCfg?.BelongVariantInfo?.SceneName ?? string.Empty))
        {
            Debug.LogError("CoLoadWorld area SceneName empty.");
            // todo 处理异常情况
            // 保底
            yield break;
        }

        currentOverlayMapId = mapOverlayId;
        cacheAreaOverlayInfo = mapOverlayCfg;

        string unitySceneName = mapOverlayCfg?.BelongVariantInfo?.SceneName;

        do
        {
            if (!IsInBuildSettings(unitySceneName))
            {
                Debug.LogError($"SubSceneManager: scene '{unitySceneName}' not in Build Settings.");
                continue;
            }
            var op = SceneManager.LoadSceneAsync(unitySceneName, LoadSceneMode.Additive);
            if (op == null) { Debug.LogError($"LoadSceneAsync returned null for {unitySceneName}"); continue; }
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                OnLoadingProgress?.Invoke(unitySceneName, op.progress);
                yield return null;
            }

            var scene = SceneManager.GetSceneByName(unitySceneName);
            if (scene.IsValid()) loadedSubScenes.Add(scene);
            else Debug.LogError($"Loaded scene invalid: {unitySceneName}");
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

                MainGameManager.Instance?.gameLogicManager?.AreaManager?.ApplyMapVariantPresentation(currentRoot);
            }
            else
            {
                // 若指定的 activeSubScene未加载，默认设为第一个加载的
                if (loadedSubScenes.Count > 0)
                    SceneManager.SetActiveScene(loadedSubScenes[0]);
            }
        }

        OnWorldLoaded?.Invoke(unitySceneName);
        Debug.Log($"SubSceneManager: World '{unitySceneName}' loaded with {loadedSubScenes.Count} sub-scenes.");

        SegmentProvider.OnAreaEnter();
        onComplete?.Invoke(0, true);
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

        var lastAreaId = currentOverlayMapId;
        currentOverlayMapId = null;
        OnWorldUnloaded?.Invoke(0);
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

    // 世界坐标判定入口（须与各 Tilemap 自身 Transform 一致：用 Grid 的 WorldToCell 会在子 Tilemap 偏移后错位）
    public bool IsWorldPosWalkable(Vector3 worldPos)
    {
        if (currentRoot == null) return false;

        if (currentRoot.HasWalkTileGrounds)
        {
            return currentRoot.IsWorldPosWalkableOnTileGrounds(worldPos);
        }

        var chunkManager = SceneAOIManager.Instance != null
            ? SceneAOIManager.Instance.MapChunkManager
            : null;
        var chunkDb = MainGameManager.Instance?.gameLogicManager?.AreaManager?.cacheChunkDatabase;
        if (chunkManager != null && chunkDb != null && chunkDb.HasChunkContent && !chunkDb.HasWalkGrid)
        {
            return chunkManager.IsWorldPosWalkable(worldPos);
        }

        return WorldAreaRoot.IsWorldPosWalkable(worldPos, currentRoot.TileGrounds, currentRoot.TileHole);
    }


    #endregion

}