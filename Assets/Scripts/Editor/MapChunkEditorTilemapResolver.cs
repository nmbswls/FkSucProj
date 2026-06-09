using System.Linq;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class MapChunkEditorUtility
{
    public static MapChunkEditorRoot Resolve(GameObject sceneRoot)
    {
        if (sceneRoot == null)
        {
            return null;
        }

        var editor = sceneRoot.GetComponent<MapChunkEditorRoot>();
        if (editor != null)
        {
            return editor;
        }

        return sceneRoot.GetComponentInParent<MapChunkEditorRoot>();
    }

    public static MapChunkEditorRoot FindInActiveScene()
    {
        MapChunkEditorRoot fallback = null;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var editor = root.GetComponentInChildren<MapChunkEditorRoot>(true);
                if (editor == null)
                {
                    continue;
                }

                if (scene.name.EndsWith("_Editor"))
                {
                    return editor;
                }

                fallback ??= editor;
            }
        }

        return fallback ?? Object.FindObjectOfType<MapChunkEditorRoot>();
    }

    public static void SyncChunkSettings(MapChunkEditorRoot editor, ref float chunkCellSize, ref Vector2 chunkOrigin)
    {
        if (editor == null)
        {
            return;
        }

        chunkCellSize = MapChunkEditorSettings.GetOrCreate().EffectiveChunkWorldSize;
        chunkOrigin = editor.ChunkOrigin;
    }

    public static void PushChunkSettings(MapChunkEditorRoot editor, float chunkCellSize, Vector2 chunkOrigin)
    {
        if (editor == null)
        {
            return;
        }

        var settings = MapChunkEditorSettings.GetOrCreate();
        settings.ChunkWorldSize = chunkCellSize;
        editor.ChunkOrigin = chunkOrigin;
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(editor);
    }

    public static string ResolveMapChunkKey(MapChunkEditorRoot editor)
    {
        if (editor == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(editor.MapVariantSceneName))
        {
            return editor.MapVariantSceneName.Trim();
        }

        return MapVariantMapResources.ResolveMapChunkKey(editor.gameObject.scene.name);
    }

    public static string ResolveMapChunkKeyFromActiveScene()
    {
        return MapVariantMapResources.ResolveMapChunkKey(
            EditorSceneManager.GetActiveScene().name);
    }
}

// 导出时从 Editor 场景层级解析 Tilemap
public static class MapChunkEditorTilemapResolver
{
    public static bool TryResolveTileGrounds(MapChunkEditorRoot editorRoot, out Tilemap[] tileGrounds)
    {
        tileGrounds = null;
        if (editorRoot == null)
        {
            return false;
        }

        var mapVariantRoot = editorRoot.MapVariantRoot;
        if (mapVariantRoot == null)
        {
            return false;
        }

        var gridRoot = mapVariantRoot.Find(MapVariantSceneHierarchy.GridRootName);
        Grid grid = null;
        if (gridRoot != null)
        {
            grid = gridRoot.GetComponent<Grid>();
        }
        else
        {
            grid = mapVariantRoot.GetComponentInChildren<Grid>(true);
        }

        if (grid == null)
        {
            return false;
        }

        Tilemap tileHole = null;
        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            if (tm != null && tm.name == "Hole")
            {
                tileHole = tm;
                break;
            }
        }

        if (editorRoot.GroundLayerNames != null && editorRoot.GroundLayerNames.Length > 0)
        {
            tileGrounds = WorldAreaRoot.CollectGroundTilemaps(grid, tileHole, editorRoot.GroundLayerNames);
            if (tileGrounds != null && tileGrounds.Length > 0)
            {
                return true;
            }
        }

        tileGrounds = grid.GetComponentsInChildren<Tilemap>(true)
            .Where(t => t != null && t != tileHole)
            .ToArray();
        return tileGrounds.Length > 0;
    }

    public static bool HasTilemapSource(MapChunkEditorRoot editorRoot)
    {
        return TryResolveTileGrounds(editorRoot, out _);
    }

    public static Transform TryGetGridRoot(MapChunkEditorRoot editorRoot)
    {
        if (editorRoot == null)
        {
            return null;
        }

        var mapVariantRoot = editorRoot.MapVariantRoot;
        if (mapVariantRoot == null)
        {
            return null;
        }

        var gridRoot = mapVariantRoot.Find(MapVariantSceneHierarchy.GridRootName);
        if (gridRoot != null)
        {
            return gridRoot;
        }

        var grid = mapVariantRoot.GetComponentInChildren<Grid>(true);
        return grid != null ? grid.transform : null;
    }

}

