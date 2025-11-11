using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using My.Map.Scene;

public class WorldAreaManager : MonoBehaviour
{
    public static WorldAreaManager Instance { get; private set; }

    [Header("Runtime")]
    public WorldAreaInfo currentWorld;
    public WorldAreaRoot currentRoot;


    public readonly List<Scene> loadedSubScenes = new List<Scene>();

    public event Action<WorldAreaInfo> OnWorldLoaded;
    public event Action<WorldAreaInfo> OnWorldUnloaded;
    public event Action<string, float> OnLoadingProgress; // 子场景名，进度0-1

    public ObstacleSegmentProvider SegmentProvider;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SegmentProvider = GetComponent<ObstacleSegmentProvider>();

        DontDestroyOnLoad(gameObject);
    }

    public bool IsWorldLoaded => currentWorld != null;

    public void LoadWorld(WorldAreaInfo areaInfo, bool setActive = true, Action<WorldAreaInfo>? onComplete = null)
    {
        StartCoroutine(CoLoadWorld(areaInfo, setActive, onComplete));
    }

    public void UnloadCurrentWorld()
    {
        if (currentWorld == null) return;
        StartCoroutine(CoUnloadWorld());
        MainGameManager.Instance.SceneFadeManager.OnLeaveArea();
        currentRoot = null;
    }

    public void Reload()
    {
        if (currentWorld == null) return;
        LoadWorld(currentWorld);
    }

    private IEnumerator CoLoadWorld(WorldAreaInfo areaInfo, bool setActive, Action<WorldAreaInfo>? onComplete)
    {
        // 先卸载旧的
        if (currentWorld != null)
            yield return CoUnloadWorld();

        currentWorld = areaInfo;
        loadedSubScenes.Clear();

        // 异步依次加载子场景（也可并行）
        foreach (var sceneName in areaInfo.subScenes)
        {
            if (!IsInBuildSettings(sceneName))
            {
                Debug.LogError($"SubSceneManager: scene '{sceneName}' not in Build Settings.");
                continue;
            }
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null) { Debug.LogError($"LoadSceneAsync returned null for {sceneName}"); continue; }
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                OnLoadingProgress?.Invoke(sceneName, op.progress);
                yield return null;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid()) loadedSubScenes.Add(scene);
            else Debug.LogError($"Loaded scene invalid: {sceneName}");
        }
        GameObject onlyRoot = null;
        // 设置激活场景（影响 Instantiate 的默认归属、Lighting、NavMesh 等）
        if (setActive && !string.IsNullOrEmpty(areaInfo.activeSubScene))
        {
            var active = loadedSubScenes.FirstOrDefault(s => s.name == areaInfo.activeSubScene);
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

        OnWorldLoaded?.Invoke(areaInfo);
        Debug.Log($"SubSceneManager: World '{areaInfo.worldName}' loaded with {loadedSubScenes.Count} sub-scenes.");

        SegmentProvider.OnAreaEnter();
        onComplete?.Invoke(areaInfo);
    }

    private IEnumerator CoUnloadWorld()
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

        var last = currentWorld;
        currentWorld = null;
        OnWorldUnloaded?.Invoke(last);
        Debug.Log("SubSceneManager: world unloaded.");
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
}