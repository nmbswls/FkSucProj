using System.Linq;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        return Object.FindObjectOfType<MapChunkEditorRoot>();
    }

    public static void SyncChunkSettings(MapChunkEditorRoot editor, ref float chunkCellSize, ref Vector2 chunkOrigin)
    {
        if (editor == null)
        {
            return;
        }

        chunkCellSize = editor.ChunkWorldSize;
        chunkOrigin = editor.ChunkOrigin;
    }

    public static void PushChunkSettings(MapChunkEditorRoot editor, float chunkCellSize, Vector2 chunkOrigin)
    {
        if (editor == null)
        {
            return;
        }

        editor.ChunkWorldSize = chunkCellSize;
        editor.ChunkOrigin = chunkOrigin;
        EditorUtility.SetDirty(editor);
    }

    public static string ResolveMapChunkKey(MapChunkEditorRoot editor)
    {
        if (editor == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(editor.SceneName))
        {
            return editor.SceneName.Trim();
        }

        return MapVariantMapResources.ResolveMapChunkKey(editor.gameObject.scene.name);
    }

    public static string ResolveMapChunkKeyFromActiveScene()
    {
        return MapVariantMapResources.ResolveMapChunkKey(
            EditorSceneManager.GetActiveScene().name);
    }
}

// 导出时从 Editor 场景层级解析 Tilemap，不写入 MapChunkEditorRoot
public static class MapChunkEditorTilemapResolver{
    public static bool TryResolveTileGrounds(MapChunkEditorRoot editorRoot, out Tilemap[] tileGrounds)
    {
        tileGrounds = null;
        if (editorRoot == null)
        {
            return false;
        }

        var worldArea = editorRoot.GetComponent<WorldAreaRoot>();
        if (worldArea != null && worldArea.TileGrounds != null && worldArea.TileGrounds.Length > 0)
        {
            tileGrounds = worldArea.TileGrounds;
            return true;
        }

        if (editorRoot.StaticPrefabRoot == null)
        {
            return false;
        }

        var gridRoot = editorRoot.StaticPrefabRoot.Find("GridRoot");
        if (gridRoot != null)
        {
            tileGrounds = gridRoot.GetComponentsInChildren<Tilemap>(true);
            return tileGrounds.Length > 0;
        }

        var grid = editorRoot.StaticPrefabRoot.GetComponentInChildren<Grid>(true);
        if (grid == null)
        {
            return false;
        }

        tileGrounds = grid.GetComponentsInChildren<Tilemap>(true);
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

        var worldArea = editorRoot.GetComponent<WorldAreaRoot>();
        if (worldArea != null && worldArea.Grid != null)
        {
            return worldArea.Grid.transform;
        }

        if (editorRoot.StaticPrefabRoot == null)
        {
            return null;
        }

        var gridRoot = editorRoot.StaticPrefabRoot.Find("GridRoot");
        if (gridRoot != null)
        {
            return gridRoot;
        }

        var grid = editorRoot.StaticPrefabRoot.GetComponentInChildren<Grid>(true);
        return grid != null ? grid.transform : null;
    }
}
