using System;
using System.Linq;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 从运行时场景复制 GridRoot 到 Editor 场景，供 Tilemap chunk 导出
public static class MapChunkEditorSceneSetup
{
    const string RuntimeScenePath = "Assets/Scenes/Main/Main_Area_01.unity";

    public static bool CopyGridFromRuntimeScene(MapChunkEditorRoot editorRoot)
    {
        if (editorRoot == null)
        {
            EditorUtility.DisplayDialog("Map Chunk Setup", "MapChunkEditorRoot is missing.", "OK");
            return false;
        }

        if (editorRoot.StaticPrefabRoot == null)
        {
            EditorUtility.DisplayDialog("Map Chunk Setup", "StaticPrefabRoot is not assigned on MapChunkEditorRoot.", "OK");
            return false;
        }

        var staticRoot = editorRoot.StaticPrefabRoot;
        if (staticRoot.Find("GridRoot") != null)
        {
            EditorUtility.DisplayDialog("Map Chunk Setup", "GridRoot already exists under StaticRoot.", "OK");
            return false;
        }

        var editorScene = editorRoot.gameObject.scene;
        Scene runtimeScene = default;
        bool openedAdditive = false;

        try
        {
            runtimeScene = SceneManager.GetSceneByPath(RuntimeScenePath);
            if (!runtimeScene.isLoaded)
            {
                runtimeScene = EditorSceneManager.OpenScene(RuntimeScenePath, OpenSceneMode.Additive);
                openedAdditive = true;
            }

            WorldAreaRoot runtimeArea = null;
            foreach (var root in runtimeScene.GetRootGameObjects())
            {
                runtimeArea = root.GetComponentInChildren<WorldAreaRoot>(true);
                if (runtimeArea != null)
                {
                    break;
                }
            }

            if (runtimeArea == null || runtimeArea.Grid == null)
            {
                EditorUtility.DisplayDialog("Map Chunk Setup", "Grid not found in runtime scene.", "OK");
                return false;
            }

            runtimeArea.ApplyTileGroundsFromLogicHeightConfig();

            var clone = UnityEngine.Object.Instantiate(runtimeArea.Grid.gameObject);
            clone.name = "GridRoot";
            clone.transform.SetParent(staticRoot, false);

            var layerNames = runtimeArea.LogicHeightConfig?.GroundLayerNames != null &&
                             runtimeArea.LogicHeightConfig.GroundLayerNames.Length > 0
                ? runtimeArea.LogicHeightConfig.GroundLayerNames.ToHashSet()
                : runtimeArea.TileGrounds
                    .Where(t => t != null)
                    .Select(t => t.name)
                    .ToHashSet();
            var tilemaps = clone.GetComponentsInChildren<Tilemap>(true)
                .Where(t => layerNames.Count == 0 || layerNames.Contains(t.name))
                .ToArray();

            Undo.RegisterCreatedObjectUndo(clone, "Copy GridRoot");
            EditorSceneManager.MarkSceneDirty(editorScene);

            Debug.Log($"[MapChunkSetup] Copied GridRoot with {tilemaps.Length} tilemap layer(s) to {editorScene.name}.");
            return true;
        }
        finally
        {
            if (openedAdditive && runtimeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(runtimeScene, true);
            }
        }
    }
}

[Obsolete("Use Window/Map Exporter instead.")]
public class MapChunkExporterWindow : EditorWindow
{
    [MenuItem("Window/Map Chunk Exporter")]
    public static void Open()
    {
        MapExporterWindow.Open();
    }
}
