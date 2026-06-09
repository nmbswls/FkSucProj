using System.IO;
using System.Linq;
using My.MapExport;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 创建运行时 + Editor 成对区域场景
public static class MapAreaSceneCreator
{
    const string SceneFolder = "Assets/Scenes/Main";
    [MenuItem("Window/Map/创建区域场景")]
    public static void ShowCreateDialog()
    {
        var window = EditorWindow.GetWindow<MapAreaSceneCreateWindow>(true, "创建区域场景", true);
        window.minSize = new Vector2(360, 96);
        window.Show();
    }

    public static bool TryCreate(string rawName, out string error)
    {
        error = null;
        var sceneName = SanitizeSceneName(rawName);
        if (string.IsNullOrEmpty(sceneName))
        {
            error = "场景名无效。";
            return false;
        }

        if (!EnsureSceneFolder())
        {
            error = $"无法创建目录：{SceneFolder}";
            return false;
        }

        var runtimePath = $"{SceneFolder}/{sceneName}.unity";
        var editorPath = $"{SceneFolder}/{sceneName}_Editor.unity";
        if (File.Exists(runtimePath) || File.Exists(editorPath))
        {
            error = "同名场景已存在，请换一个名称。";
            return false;
        }

        var previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            CreateRuntimeScene(sceneName, runtimePath);
            CreateEditorScene(sceneName, editorPath);
            AddRuntimeSceneToBuildSettings(runtimePath);
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(editorPath, OpenSceneMode.Single);
            var areaRoot = Object.FindObjectOfType<MapChunkEditorRoot>();
            if (areaRoot != null)
            {
                Selection.activeGameObject = areaRoot.gameObject;
            }

            EditorUtility.DisplayDialog(
                "创建完成",
                $"已创建：\n{runtimePath}\n{editorPath}\n\n运行时场景已加入 Build Settings。",
                "OK");
            return true;
        }
        catch (System.Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            if (previousSetup != null && previousSetup.Length > 0)
            {
                // 成功时已打开 Editor 场景；失败则恢复先前场景布局
                if (!string.IsNullOrEmpty(error))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }
        }
    }

    static bool EnsureSceneFolder()
    {
        if (AssetDatabase.IsValidFolder(SceneFolder))
        {
            return true;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        if (!AssetDatabase.IsValidFolder(SceneFolder))
        {
            AssetDatabase.CreateFolder("Assets/Scenes", "Main");
        }

        return AssetDatabase.IsValidFolder(SceneFolder);
    }

    static void CreateRuntimeScene(string sceneName, string savePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;

        var areaRootGo = new GameObject("AreaRoot");
        areaRootGo.AddComponent<WorldAreaRoot>();
        var staticRoot = CreateChild(areaRootGo.transform, "StaticRoot");
        CreateSceneGridRoot(staticRoot);
        CreateNavMeshSurface(areaRootGo.transform);

        EditorSceneManager.SaveScene(scene, savePath);
    }

    static void CreateEditorScene(string runtimeSceneName, string savePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = $"{runtimeSceneName}_Editor";

        var areaRootGo = new GameObject("AreaRoot");
        var editorRoot = areaRootGo.AddComponent<MapChunkEditorRoot>();
        editorRoot.SceneName = runtimeSceneName;

        var staticRoot = CreateChild(areaRootGo.transform, "StaticRoot");
        editorRoot.StaticPrefabRoot = staticRoot;
        CreateGridRoot(staticRoot);

        CreateChild(areaRootGo.transform, "NamedPath");
        CreateChild(areaRootGo.transform, "NamedPoint");
        CreateChild(areaRootGo.transform, "DynamicRoot");
        CreateChild(areaRootGo.transform, "FovObstacleRoot");

        var portalNetworks = CreateChild(areaRootGo.transform, "PortalNetworks");
        portalNetworks.gameObject.AddComponent<PortalNetworkProvider>();

        CreateNavMeshSurface(areaRootGo.transform);
        CreateChild(areaRootGo.transform, "NavObc");
        CreateChild(areaRootGo.transform, "TriggerArea");

        var roomRoot = CreateChild(areaRootGo.transform, "RoomRoot");
        roomRoot.gameObject.SetActive(false);

        CreateChild(areaRootGo.transform, "Col");

        EditorSceneManager.SaveScene(scene, savePath);
    }

    static Transform CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void CreateSceneGridRoot(Transform staticRoot)
    {
        var gridRootGo = new GameObject(WorldAreaRoot.SceneGridRootName);
        gridRootGo.transform.SetParent(staticRoot, false);
        var grid = gridRootGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
    }

    static void CreateGridRoot(Transform staticRoot)
    {
        var gridRootGo = new GameObject(WorldAreaRoot.SceneGridRootName);
        gridRootGo.transform.SetParent(staticRoot, false);
        gridRootGo.SetActive(false);

        var grid = gridRootGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        CreateTilemapLayer(gridRootGo.transform, "Ground");
        CreateTilemapLayer(gridRootGo.transform, "Hole");
    }

    static void CreateTilemapLayer(Transform parent, string layerName)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(parent, false);
        go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
    }

    static void CreateNavMeshSurface(Transform parent)
    {
        var go = new GameObject("NavMesh Surface");
        go.transform.SetParent(parent, false);
        go.AddComponent<NavMeshSurface>();
    }

    static void AddRuntimeSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static string SanitizeSceneName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var name = raw.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid.ToString(), string.Empty);
        }

        return string.IsNullOrEmpty(name) ? null : name;
    }
}

class MapAreaSceneCreateWindow : EditorWindow
{
    string sceneName = "Main_Area_02";

    void OnGUI()
    {
        EditorGUILayout.LabelField("场景基础名（不含 _Editor 后缀）");
        sceneName = EditorGUILayout.TextField("名称", sceneName);
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("取消", GUILayout.Width(72)))
        {
            Close();
        }

        if (GUILayout.Button("创建", GUILayout.Width(72)))
        {
            if (MapAreaSceneCreator.TryCreate(sceneName, out var error))
            {
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("创建失败", error, "OK");
            }
        }

        EditorGUILayout.EndHorizontal();
    }
}
