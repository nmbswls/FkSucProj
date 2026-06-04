#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public static ImportResult ImportPaintedAtlas(
        MapChunkEditorRoot root,
        string mapName,
        Texture2D paintedAtlas,
        FilterMode resampleFilter = FilterMode.Bilinear)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (paintedAtlas == null)
        {
            return Fail("Painted atlas texture is missing.");
        }

        string manifestPath = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(manifestPath);
        if (manifest == null)
        {
            return Fail("Paint manifest not found. Generate Paint Atlas first.");
        }

        if (manifest.AtlasWidth <= 0 || manifest.AtlasHeight <= 0 || manifest.SlicePixelSize <= 0)
        {
            return Fail("Manifest atlas size is invalid.");
        }

        if (paintedAtlas.width != manifest.AtlasWidth || paintedAtlas.height != manifest.AtlasHeight)
        {
            return Fail(
                $"Atlas size mismatch: expected {manifest.AtlasWidth}x{manifest.AtlasHeight}, " +
                $"got {paintedAtlas.width}x{paintedAtlas.height}.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        BackupCurrentRevision(mapName, manifest.ExportRevision);

        var layout = ComputeLayoutFromManifest(manifest, root.PaintWorldRect);
        string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);
        int imported = 0;

        var readable = EnsureReadable(paintedAtlas);
        try
        {
            foreach (var coord in layout.ChunkCoords)
            {
                int col = coord.X - layout.MinCoord.X;
                int row = coord.Y - layout.MinCoord.Y;
                int x = col * manifest.SlicePixelSize;
                int y = row * manifest.SlicePixelSize;

                var chunkTex = new Texture2D(manifest.SlicePixelSize, manifest.SlicePixelSize, TextureFormat.RGBA32, false);
                chunkTex.SetPixels(readable.GetPixels(x, y, manifest.SlicePixelSize, manifest.SlicePixelSize));
                chunkTex.Apply();

                string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
                MapPaintBackgroundShared.WritePng(chunkTex, paintedPath);
                AssetDatabase.ImportAsset(paintedPath);
                Object.DestroyImmediate(chunkTex);

                var info = manifest.GetOrCreateChunk(coord);
                info.Source = ChunkPaintSource.UserPainted;
                info.ResetOnExport = false;

                UpdateBackgroundFromPainted(root, mapName, coord, paintedPath, resampleFilter, rootFolder);
                imported++;
            }
        }
        finally
        {
            if (!ReferenceEquals(readable, paintedAtlas))
            {
                Object.DestroyImmediate(readable);
            }
        }

        manifest.ExportRevision++;
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new ImportResult
        {
            Success = true,
            Message = $"Imported painted atlas into {imported} chunk(s).",
        };
    }

    public static ImportResult ImportSingleChunk(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        Texture2D paintedChunk,
        FilterMode resampleFilter = FilterMode.Bilinear)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (paintedChunk == null)
        {
            return Fail("Painted chunk texture is missing.");
        }

        string manifestPath = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(manifestPath);
        if (manifest == null)
        {
            return Fail("Paint manifest not found. Generate Paint Atlas first.");
        }

        if (manifest.SlicePixelSize <= 0)
        {
            return Fail("Manifest slice size is invalid.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        BackupSingleChunk(mapName, manifest.ExportRevision, coord);

        var chunkTex = MapPaintBackgroundShared.ResampleTexture(
            paintedChunk,
            manifest.SlicePixelSize,
            manifest.SlicePixelSize,
            resampleFilter);

        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
        MapPaintBackgroundShared.WritePng(chunkTex, paintedPath);
        Object.DestroyImmediate(chunkTex);
        AssetDatabase.ImportAsset(paintedPath);

        var info = manifest.GetOrCreateChunk(coord);
        info.Source = ChunkPaintSource.UserPainted;
        info.ResetOnExport = false;

        string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);
        UpdateBackgroundFromPainted(root, mapName, coord, paintedPath, resampleFilter, rootFolder);

        manifest.ExportRevision++;
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new ImportResult
        {
            Success = true,
            Message = $"Imported painted chunk ({coord.X}, {coord.Y}).",
        };
    }

    public static ImportResult ImportSingleChunkWithContext(
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
            return Fail("Context chunk texture is missing.");
        }

        string manifestPath = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(manifestPath);
        if (manifest == null)
        {
            return Fail("Paint manifest not found. Generate Paint Atlas or export single chunk first.");
        }

        if (manifest.SlicePixelSize <= 0)
        {
            return Fail("Manifest slice size is invalid.");
        }

        float expandRatio = root.PaintContextExpandRatio;
        if (manifest.ContextExpandRatio > 0f)
        {
            expandRatio = manifest.ContextExpandRatio;
        }

        int expected = MapPaintBackgroundContext.ComputeContextSize(manifest.SlicePixelSize, expandRatio);
        if (contextChunk.width != expected || contextChunk.height != expected)
        {
            return Fail(
                $"Context size mismatch: expected {expected}x{expected} " +
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
                return Fail("Failed to crop center from context image.");
            }

            string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
            MapPaintBackgroundShared.WritePng(cropped, paintedPath);
            AssetDatabase.ImportAsset(paintedPath);

            var info = manifest.GetOrCreateChunk(coord);
            info.Source = ChunkPaintSource.UserPainted;
            info.ResetOnExport = false;

            string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);
            UpdateBackgroundFromPainted(root, mapName, coord, paintedPath, resampleFilter, rootFolder);

            manifest.ExportRevision++;
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new ImportResult
            {
                Success = true,
                Message =
                    $"Imported chunk ({coord.X}, {coord.Y}) from context image; " +
                    $"cropped center {manifest.SlicePixelSize}px (removed {expandRatio:P0} margin).",
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

    struct AtlasLayout
    {
        public ChunkCoord MinCoord;
        public List<ChunkCoord> ChunkCoords;
    }

    static AtlasLayout ComputeLayoutFromManifest(MapPaintManifest manifest, Rect paintRect)
    {
        var minCoord = MapChunkUtility.WorldToChunk(new Vector2(paintRect.xMin, paintRect.yMin), manifest.ChunkOrigin, manifest.ChunkWorldSize);
        var maxCoord = MapChunkUtility.WorldToChunk(
            new Vector2(paintRect.xMax - 1e-4f, paintRect.yMax - 1e-4f),
            manifest.ChunkOrigin,
            manifest.ChunkWorldSize);

        var coords = new List<ChunkCoord>();
        for (int cy = minCoord.Y; cy <= maxCoord.Y; cy++)
        {
            for (int cx = minCoord.X; cx <= maxCoord.X; cx++)
            {
                coords.Add(new ChunkCoord(cx, cy));
            }
        }

        return new AtlasLayout { MinCoord = minCoord, ChunkCoords = coords };
    }

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
        float bgPpu = root.TexturePPU > 0f ? root.TexturePPU : root.EffectivePaintExportPpu;
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
            MapPaintBackgroundShared.SaveBackgroundPrefab(coord, spritePath, rootFolder, root.BackgroundSortingOrder);
            AssetDatabase.ImportAsset(spritePath);
        }
    }

    static void BackupCurrentRevision(string mapName, int revision)
    {
        string backupFolder = MapPaintBackgroundShared.GetBackupFolder(mapName, revision);
        MapChunkExportCore.EnsureFolderPublic(backupFolder);

        string atlasPath = MapPaintBackgroundShared.GetAtlasPath(mapName);
        if (File.Exists(atlasPath))
        {
            File.Copy(atlasPath, $"{backupFolder}/{MapPaintBackgroundShared.AtlasFileName}", true);
        }

        string chunksFolder = MapPaintBackgroundShared.GetChunksFolder(mapName);
        if (!Directory.Exists(chunksFolder))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(chunksFolder, "painted_*.png"))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, $"{backupFolder}/{name}", true);
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
