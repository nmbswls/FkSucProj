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

    // 从外部 PNG 导入到 painted 目录（不裁剪、不打包；Sync 时再处理）
    public static ImportResult ImportPaintedPngFromFile(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        string externalPath = EditorUtility.OpenFilePanel(
            $"Import Painted PNG ({coord.X},{coord.Y})",
            "",
            "png");
        if (string.IsNullOrEmpty(externalPath))
        {
            return Fail("Import cancelled.");
        }

        return ImportPaintedPng(root, mapName, coord, externalPath);
    }

    // Path-based entry point for Codex/batch automation. The editor window keeps using the file picker above.
    public static ImportResult ImportPaintedPng(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        string externalPath)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (!File.Exists(externalPath))
        {
            return Fail("Selected file does not exist.");
        }

        var manifest = EnsureManifest(root, mapName, out var slicePx, out var expandRatio);
        if (manifest == null)
        {
            return Fail("Failed to load paint manifest.");
        }

        int contextSize = MapPaintBackgroundContext.ComputeContextSize(slicePx, expandRatio);
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(externalPath);
        }
        catch (IOException ex)
        {
            return Fail($"Failed to read file: {ex.Message}");
        }

        var tex = MapPaintBackgroundShared.LoadTextureFromBytes(bytes);
        if (tex == null)
        {
            return Fail("Selected file is not a valid PNG image.");
        }

        try
        {
            bool sizeOk = tex.width == tex.height && tex.width >= slicePx;
            if (!sizeOk)
            {
                return Fail(
                    $"Image size mismatch: expected a square PNG at least {slicePx}x{slicePx} " +
                    $"(native context {contextSize}x{contextSize}, expand {expandRatio:P0}), " +
                    $"got {tex.width}x{tex.height}.");
            }

            MapPaintBackgroundShared.EnsurePaintFolders(mapName);
            BackupSingleChunk(mapName, manifest.ExportRevision, coord);

            string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
            File.WriteAllBytes(paintedPath, bytes);
            AssetDatabase.ImportAsset(paintedPath);

            var info = manifest.GetOrCreateChunk(coord);
            info.Source = ChunkPaintSource.UserPainted;
            manifest.ExportRevision++;
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();

            MapPaintBackgroundPreview.TryAutoSync(root, mapName);

            return new ImportResult
            {
                Success = true,
                Message =
                    $"Imported painted PNG ({coord.X},{coord.Y}) -> {paintedPath}. " +
                    "Use Sync to crop and pack runtime backgrounds.",
            };
        }
        finally
        {
            Object.DestroyImmediate(tex);
        }
    }

    static MapPaintManifest EnsureManifest(
        MapChunkEditorRoot root,
        string mapName,
        out int slicePx,
        out float expandRatio)
    {
        slicePx = 0;
        expandRatio = MapChunkEditorSettings.GetOrCreate().PaintContextExpandRatio;

        string manifestPath = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(manifestPath);
        if (manifest == null)
        {
            MapPaintBackgroundShared.EnsurePaintFolders(mapName);
            var settings = MapChunkEditorSettings.GetOrCreate();
            float paintPpu = settings.EffectivePaintExportPpu;
            slicePx = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, paintPpu);
            manifest = ScriptableObject.CreateInstance<MapPaintManifest>();
            manifest.SceneName = mapName;
            manifest.ChunkWorldSize = root.ChunkWorldSize;
            manifest.ChunkOrigin = root.ChunkOrigin;
            manifest.TexturePPU = settings.TexturePPU;
            manifest.PaintExportPPU = paintPpu;
            manifest.SlicePixelSize = slicePx;
            manifest.ContextExpandRatio = expandRatio;
            manifest.MaskColor = settings.PaintMaskColor;
            manifest.PaintWorldRect = root.PaintWorldRect;
            AssetDatabase.CreateAsset(manifest, manifestPath);
        }
        else
        {
            slicePx = manifest.SlicePixelSize > 0
                ? manifest.SlicePixelSize
                : MapChunkUtility.ComputeSlicePixelSize(
                    root.ChunkWorldSize,
                    MapChunkEditorSettings.GetOrCreate().EffectivePaintExportPpu);
            if (manifest.ContextExpandRatio > 0f)
            {
                expandRatio = manifest.ContextExpandRatio;
            }
        }

        return manifest;
    }

    static ImportResult Fail(string message) => new ImportResult { Success = false, Message = message };

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
