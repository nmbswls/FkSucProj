#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 在 Editor 场景按 chunk 网格实例化 bg_*，预览 Import / Generate 结果
public static class MapPaintBackgroundPreview
{
    public const string PreviewRootName = "PaintBackgroundPreview";

    public struct SyncResult
    {
        public bool Success;
        public string Message;
        public int SyncedCount;
    }

    public static SyncResult SyncToScene(MapChunkEditorRoot root, string mapName)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (root.PaintWorldRect.width <= 0f || root.PaintWorldRect.height <= 0f)
        {
            return Fail("PaintWorldRect is not configured.");
        }

        var previewRoot = GetOrCreatePreviewRoot(root);
        previewRoot.gameObject.SetActive(root.PaintPreviewEnabled);

        if (!root.PaintPreviewEnabled)
        {
            return new SyncResult
            {
                Success = true,
                Message = "Paint preview hidden.",
                SyncedCount = 0,
            };
        }

        var coords = new HashSet<ChunkCoord>();
        MapPaintBackgroundShared.CollectPaintRectCoords(root, coords);

        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(
            MapPaintBackgroundShared.GetManifestPath(mapName));

        var keepNames = new HashSet<string>();
        int synced = 0;
        int skipped = 0;

        foreach (var coord in coords)
        {
            string instanceName = BuildInstanceName(coord);
            keepNames.Add(instanceName);

            string prefabPath = $"{MapPaintBackgroundShared.GetMapRootFolder(mapName)}/Prefabs/bg_{coord.X}_{coord.Y}.prefab";
            if (!File.Exists(prefabPath))
            {
                RemoveChildIfExists(previewRoot, instanceName);
                skipped++;
                continue;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                skipped++;
                continue;
            }

            var chunkMin = MapChunkUtility.ChunkWorldMin(coord, root.ChunkOrigin, root.ChunkWorldSize);
            var existing = previewRoot.Find(instanceName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, previewRoot) as GameObject;
            if (instance == null)
            {
                skipped++;
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Sync Paint Preview");
            instance.name = instanceName;
            instance.transform.position = chunkMin;

            var sr = instance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = root.BackgroundSortingOrder;
            }

            synced++;
        }

        for (int i = previewRoot.childCount - 1; i >= 0; i--)
        {
            var child = previewRoot.GetChild(i);
            if (!keepNames.Contains(child.name))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        EditorUtility.SetDirty(previewRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);

        string manifestHint = manifest != null ? $" revision {manifest.ExportRevision}" : string.Empty;
        return new SyncResult
        {
            Success = true,
            Message = $"Paint preview synced: {synced} chunk(s), skipped {skipped}.{manifestHint}",
            SyncedCount = synced,
        };
    }

    public static SyncResult ClearPreview(MapChunkEditorRoot root)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        var previewRoot = root.transform.Find(PreviewRootName);
        if (previewRoot == null)
        {
            return new SyncResult { Success = true, Message = "Paint preview is already empty.", SyncedCount = 0 };
        }

        Undo.DestroyObjectImmediate(previewRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        return new SyncResult { Success = true, Message = "Paint preview cleared.", SyncedCount = 0 };
    }

    public static void TryAutoSync(MapChunkEditorRoot root, string mapName)
    {
        if (root == null || !root.PaintAutoRefreshPreview)
        {
            return;
        }

        SyncToScene(root, mapName);
    }

    public static Texture2D LoadPreviewTexture(string mapName, ChunkCoord coord, MapPaintManifest manifest)
    {
        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
        if (File.Exists(paintedPath))
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(paintedPath);
        }

        string templatePath = MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);
        if (File.Exists(templatePath))
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(templatePath);
        }

        string forAiPath = MapPaintBackgroundShared.GetChunkForAiPath(mapName, coord);
        if (File.Exists(forAiPath))
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(forAiPath);
        }

        string bgPath = $"{MapPaintBackgroundShared.GetMapRootFolder(mapName)}/Sprites/bg_{coord.X}_{coord.Y}.png";
        return File.Exists(bgPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(bgPath) : null;
    }

    static SyncResult Fail(string message) => new SyncResult { Success = false, Message = message };

    static Transform GetOrCreatePreviewRoot(MapChunkEditorRoot root)
    {
        var existing = root.transform.Find(PreviewRootName);
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject(PreviewRootName);
        Undo.RegisterCreatedObjectUndo(go, "Create Paint Preview Root");
        go.transform.SetParent(root.transform, false);
        return go.transform;
    }

    static string BuildInstanceName(ChunkCoord coord) => $"bg_{coord.X}_{coord.Y}";

    static void RemoveChildIfExists(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }
}
#endif
