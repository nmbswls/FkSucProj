#if UNITY_EDITOR
using System.IO;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEngine;

public static class MapPaintBackgroundImporter
{
    public struct ImportResult
    {
        public bool Success;
        public string Message;
    }

    public static ImportResult ImportChunkForAi(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        Texture2D contextChunk,
        FilterMode resampleFilter = FilterMode.Bilinear)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (contextChunk == null)
        {
            return Fail("Import texture is missing.");
        }

        string manifestPath = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(manifestPath);
        if (manifest == null)
        {
            return Fail("Paint manifest not found. Export chunk for AI first.");
        }

        if (manifest.SlicePixelSize <= 0)
        {
            return Fail("Manifest slice size is invalid.");
        }

        float expandRatio = manifest.ContextExpandRatio > 0f
            ? manifest.ContextExpandRatio
            : MapChunkEditorSettings.GetOrCreate().PaintContextExpandRatio;
        int expected = MapPaintBackgroundContext.ComputeContextSize(manifest.SlicePixelSize, expandRatio);
        if (contextChunk.width != expected || contextChunk.height != expected)
        {
            return Fail(
                $"Import size mismatch: expected {expected}x{expected} " +
                $"(slice {manifest.SlicePixelSize}, expand {expandRatio:P0}), " +
                $"got {contextChunk.width}x{contextChunk.height}.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        BackupSingleChunk(mapName, manifest.ExportRevision, coord);

        var readable = EnsureReadable(contextChunk);
        Texture2D cropped = null;
        try
        {
            cropped = MapPaintBackgroundContext.CropCenterFromContext(
                readable,
                manifest.SlicePixelSize,
                expandRatio,
                resampleFilter);
            if (cropped == null)
            {
                return Fail("Failed to crop center from import image.");
            }

            string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
            MapPaintBackgroundShared.WritePng(cropped, paintedPath);
            AssetDatabase.ImportAsset(paintedPath);

            var info = manifest.GetOrCreateChunk(coord);
            info.Source = ChunkPaintSource.UserPainted;

            string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);
            UpdateBackgroundFromPainted(root, mapName, coord, paintedPath, resampleFilter, rootFolder);

            manifest.ExportRevision++;
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MapPaintBackgroundPreview.TryAutoSync(root, mapName);

            return new ImportResult
            {
                Success = true,
                Message = $"Imported chunk ({coord.X}, {coord.Y}); cropped to {manifest.SlicePixelSize}px.",
            };
        }
        finally
        {
            if (!ReferenceEquals(readable, contextChunk))
            {
                Object.DestroyImmediate(readable);
            }

            if (cropped != null)
            {
                Object.DestroyImmediate(cropped);
            }
        }
    }

    static ImportResult Fail(string message) => new ImportResult { Success = false, Message = message };

    static Texture2D EnsureReadable(Texture2D source)
    {
        string path = AssetDatabase.GetAssetPath(source);
        if (!string.IsNullOrEmpty(path))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        if (source.isReadable)
        {
            return source;
        }

        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    static void UpdateBackgroundFromPainted(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        string paintedPath,
        FilterMode resampleFilter,
        string rootFolder)
    {
        var settings = MapChunkEditorSettings.GetOrCreate();
        float bgPpu = settings.TexturePPU > 0f ? settings.TexturePPU : settings.EffectivePaintExportPpu;
        int bgSlice = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, bgPpu);
        var src = MapPaintBackgroundShared.LoadTextureFromAssetPath(paintedPath);
        if (src == null)
        {
            return;
        }

        var bgTex = MapPaintBackgroundShared.ResampleTexture(src, bgSlice, bgSlice, resampleFilter);
        Object.DestroyImmediate(src);
        string spritePath = MapPaintBackgroundShared.SaveBackgroundSprite(bgTex, coord, bgPpu, rootFolder);
        Object.DestroyImmediate(bgTex);
        if (!string.IsNullOrEmpty(spritePath))
        {
            MapPaintBackgroundShared.SaveBackgroundPrefab(coord, spritePath, rootFolder, settings.BackgroundSortingOrder);
            AssetDatabase.ImportAsset(spritePath);
        }
    }

    static void BackupSingleChunk(string mapName, int revision, ChunkCoord coord)
    {
        string backupFolder = MapPaintBackgroundShared.GetBackupFolder(mapName, revision);
        MapChunkExportCore.EnsureFolderPublic(backupFolder);
        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
        if (File.Exists(paintedPath))
        {
            File.Copy(paintedPath, $"{backupFolder}/painted_{coord.X}_{coord.Y}.png", true);
        }
    }
}
#endif
