using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 将 Editor 场景中不需切分的骨架（GridRoot、NavMesh）同步到运行时场景
public static class MapAreaRuntimeSceneSync
{
    const string SceneFolder = "Assets/Scenes/Main";

    [MenuItem("Window/Map/同步运行时场景骨架")]
    public static void SyncActiveAreaPair()
    {
        var editorScene = EditorSceneManager.GetActiveScene();
        if (!editorScene.name.EndsWith("_Editor"))
        {
            EditorUtility.DisplayDialog("同步失败", "请先打开 *_Editor 场景。", "OK");
            return;
        }

        var runtimeName = editorScene.name.Substring(0, editorScene.name.Length - "_Editor".Length);
        var runtimePath = $"{SceneFolder}/{runtimeName}.unity";
        if (!System.IO.File.Exists(runtimePath))
        {
            EditorUtility.DisplayDialog("同步失败", $"未找到运行时场景：{runtimePath}", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!SyncFromOpenEditorScene(editorScene, runtimePath, out var message))
        {
            EditorUtility.DisplayDialog("同步失败", message, "OK");
            return;
        }

        EditorUtility.DisplayDialog("同步完成", message, "OK");
    }

    static bool SyncFromOpenEditorScene(Scene editorScene, string runtimeScenePath, out string message)
    {
        message = null;
        var runtimeScene = EditorSceneManager.OpenScene(runtimeScenePath, OpenSceneMode.Additive);
        try
        {
            var editorRoot = FindAreaRoot(editorScene);
            var runtimeRoot = FindAreaRoot(runtimeScene);
            if (editorRoot == null || runtimeRoot == null)
            {
                message = "Editor 或运行时场景中未找到 AreaRoot。";
                return false;
            }

            SyncGridRoot(editorRoot, runtimeRoot);
            SyncNavMeshSurface(editorRoot, runtimeRoot);

            EditorSceneManager.MarkSceneDirty(runtimeScene);
            EditorSceneManager.SaveScene(runtimeScene);
            message = $"已同步 {runtimeScene.name}：GridRoot（空根节点）+ NavMesh Surface。";
            return true;
        }
        finally
        {
            if (runtimeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(runtimeScene, true);
            }
        }
    }

    static GameObject FindAreaRoot(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "AreaRoot")
            {
                return root;
            }

            var nested = root.transform.Find("AreaRoot");
            if (nested != null)
            {
                return nested.gameObject;
            }
        }

        return null;
    }

    static void SyncGridRoot(GameObject editorRoot, GameObject runtimeRoot)
    {
        var editorStatic = editorRoot.transform.Find("StaticRoot");
        var runtimeStatic = runtimeRoot.transform.Find("StaticRoot");
        if (editorStatic == null || runtimeStatic == null)
        {
            Debug.LogWarning("[MapAreaRuntimeSceneSync] StaticRoot missing, skip GridRoot.");
            return;
        }

        var editorGridRoot = editorStatic.Find(WorldAreaRoot.SceneGridRootName);
        if (editorGridRoot == null)
        {
            Debug.LogWarning("[MapAreaRuntimeSceneSync] Editor GridRoot missing, skip.");
            return;
        }

        var runtimeGridRoot = runtimeStatic.Find(WorldAreaRoot.SceneGridRootName);
        if (runtimeGridRoot == null)
        {
            var go = new GameObject(WorldAreaRoot.SceneGridRootName);
            go.transform.SetParent(runtimeStatic, false);
            runtimeGridRoot = go.transform;
        }

        ClearChildren(runtimeGridRoot);

        var editorGrid = editorGridRoot.GetComponent<Grid>();
        var runtimeGrid = runtimeGridRoot.GetComponent<Grid>();
        if (runtimeGrid == null)
        {
            runtimeGrid = runtimeGridRoot.gameObject.AddComponent<Grid>();
        }

        if (editorGrid != null)
        {
            runtimeGrid.cellSize = editorGrid.cellSize;
            runtimeGrid.cellGap = editorGrid.cellGap;
            runtimeGrid.cellLayout = editorGrid.cellLayout;
            runtimeGrid.cellSwizzle = editorGrid.cellSwizzle;
        }
        else
        {
            runtimeGrid.cellSize = Vector3.one;
        }
    }

    static void SyncNavMeshSurface(GameObject editorRoot, GameObject runtimeRoot)
    {
        var editorSurface = editorRoot.GetComponentInChildren<NavMeshSurface>(true);
        if (editorSurface == null)
        {
            Debug.LogWarning("[MapAreaRuntimeSceneSync] Editor NavMeshSurface missing, skip.");
            return;
        }

        var runtimeTransform = runtimeRoot.transform.Find("NavMesh Surface");
        GameObject runtimeGo;
        if (runtimeTransform == null)
        {
            runtimeGo = new GameObject("NavMesh Surface");
            runtimeGo.transform.SetParent(runtimeRoot.transform, false);
        }
        else
        {
            runtimeGo = runtimeTransform.gameObject;
        }

        runtimeGo.transform.SetPositionAndRotation(
            editorSurface.transform.position,
            editorSurface.transform.rotation);
        runtimeGo.transform.localScale = editorSurface.transform.localScale;

        CopyNavMeshComponents(editorSurface.gameObject, runtimeGo);
    }

    static void CopyNavMeshComponents(GameObject source, GameObject dest)
    {
        var sourceComponents = source.GetComponents<Component>();
        for (int i = 0; i < sourceComponents.Length; i++)
        {
            var src = sourceComponents[i];
            if (src is Transform)
            {
                continue;
            }

            var type = src.GetType();
            var dst = dest.GetComponent(type);
            if (dst == null)
            {
                dst = dest.AddComponent(type);
            }

            EditorUtility.CopySerialized(src, dst);
        }
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
