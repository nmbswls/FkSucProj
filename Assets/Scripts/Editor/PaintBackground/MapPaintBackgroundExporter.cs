#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEngine;

public static class MapPaintBackgroundExporter
{
    public struct ExportResult
    {
        public bool Success;
        public string Message;
        public MapPaintManifest Manifest;
    }

    public static ExportResult ExportSingleChunkForAi(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        FilterMode resampleFilter = FilterMode.Bilinear)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (root.PaintWorldRect.width <= 0f || root.PaintWorldRect.height <= 0f)
        {
            return Fail("PaintWorldRect is not configured.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        var manifest = LoadOrCreateManifest(root, mapName);
        var settings = MapChunkEditorSettings.GetOrCreate();
        float paintPpu = settings.EffectivePaintExportPpu;
        int slicePx = manifest.SlicePixelSize > 0
            ? manifest.SlicePixelSize
            : MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, paintPpu);

        var info = manifest.GetOrCreateChunk(coord);
        bool hasPainted = MapPaintChunkState.HasPainted(mapName, info, coord);

        if (info.TemplateStale && !hasPainted)
        {
            return Fail($"Chunk ({coord.X},{coord.Y}) is stale. Re-capture template before export.");
        }

        float expandRatio = MapChunkEditorSettings.GetOrCreate().PaintContextExpandRatio;
        int contextSize = MapPaintBackgroundContext.ComputeContextSize(slicePx, expandRatio);
        int margin = MapPaintBackgroundContext.ComputeMarginPx(slicePx, expandRatio);

        if (info.TemplateStale && hasPainted)
        {
            return ExportStalePaintedChunkForAi(
                root, mapName, manifest, coord, slicePx, paintPpu, expandRatio, contextSize, margin, resampleFilter);
        }

        UpdateChunkTemplate(root, mapName, manifest, info, coord, paintPpu, forceCapture: false);

        Texture2D forAi = null;
        try
        {
            forAi = MapPaintBackgroundContext.BuildChunkForAi(
                mapName,
                manifest,
                coord,
                slicePx,
                expandRatio,
                MapChunkEditorSettings.GetOrCreate().PaintMaskColor,
                resampleFilter);

            string outputPath = MapPaintBackgroundShared.GetChunkForAiPath(mapName, coord);
            MapPaintBackgroundShared.WritePng(forAi, outputPath);
            AssetDatabase.ImportAsset(outputPath);

            SaveManifestState(root, manifest, mapName, slicePx, paintPpu, expandRatio);
            AssetDatabase.SaveAssets();
            MapPaintBackgroundPreview.TryAutoSync(root, mapName);

            return new ExportResult
            {
                Success = true,
                Message =
                    $"Exported chunk ({coord.X},{coord.Y}) for AI: {contextSize}x{contextSize}px " +
                    $"(center {slicePx}px + margin {margin}px).",
                Manifest = manifest,
            };
        }
        finally
        {
            if (forAi != null)
            {
                Object.DestroyImmediate(forAi);
            }
        }
    }

    public static ExportResult SetTemplateStale(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        bool stale)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        var manifest = LoadOrCreateManifest(root, mapName);
        var info = manifest.GetOrCreateChunk(coord);
        info.TemplateStale = stale;
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();

        string action = stale ? "Marked stale" : "Cleared stale";
        return new ExportResult
        {
            Success = true,
            Message = $"{action} for chunk ({coord.X},{coord.Y}).",
            Manifest = manifest,
        };
    }

    static ExportResult ExportStalePaintedChunkForAi(
        MapChunkEditorRoot root,
        string mapName,
        MapPaintManifest manifest,
        ChunkCoord coord,
        int slicePx,
        float paintPpu,
        float expandRatio,
        int contextSize,
        int margin,
        FilterMode resampleFilter)
    {
        Texture2D freshCenter = null;
        Texture2D forAi = null;
        Texture2D paintedRef = null;
        try
        {
            freshCenter = CaptureFreshTemplate(root, coord, paintPpu);
            if (freshCenter == null)
            {
                return Fail($"Failed to capture fresh template for chunk ({coord.X},{coord.Y}).");
            }

            forAi = MapPaintBackgroundContext.BuildChunkForAi(
                mapName,
                manifest,
                coord,
                slicePx,
                expandRatio,
                MapChunkEditorSettings.GetOrCreate().PaintMaskColor,
                resampleFilter,
                freshCenter);

            paintedRef = MapPaintBackgroundContext.BuildChunkForAi(
                mapName,
                manifest,
                coord,
                slicePx,
                expandRatio,
                MapChunkEditorSettings.GetOrCreate().PaintMaskColor,
                resampleFilter);

            string forAiPath = MapPaintBackgroundShared.GetChunkForAiPath(mapName, coord);
            string paintedRefPath = MapPaintBackgroundShared.GetChunkPaintedRefPath(mapName, coord);
            MapPaintBackgroundShared.WritePng(forAi, forAiPath);
            MapPaintBackgroundShared.WritePng(paintedRef, paintedRefPath);
            AssetDatabase.ImportAsset(forAiPath);
            AssetDatabase.ImportAsset(paintedRefPath);

            SaveManifestState(root, manifest, mapName, slicePx, paintPpu, expandRatio);
            AssetDatabase.SaveAssets();
            MapPaintBackgroundPreview.TryAutoSync(root, mapName);

            return new ExportResult
            {
                Success = true,
                Message =
                    $"Exported stale chunk ({coord.X},{coord.Y}): " +
                    $"{contextSize}x{contextSize}px for_ai (fresh template) + painted_ref (old painted). " +
                    "Give both to AI; import the merged result, then Re-capture.",
                Manifest = manifest,
            };
        }
        finally
        {
            if (freshCenter != null)
            {
                Object.DestroyImmediate(freshCenter);
            }

            if (forAi != null)
            {
                Object.DestroyImmediate(forAi);
            }

            if (paintedRef != null)
            {
                Object.DestroyImmediate(paintedRef);
            }
        }
    }

    static Texture2D CaptureFreshTemplate(MapChunkEditorRoot root, ChunkCoord coord, float paintPpu)
    {
        return MapPaintBackgroundCapture.CaptureChunk(
            root,
            coord,
            paintPpu,
            MapChunkEditorSettings.GetOrCreate().PaintMaskColor,
            out _);
    }

    public static ExportResult ClearUserPainted(MapChunkEditorRoot root, string mapName, ChunkCoord coord)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        var manifest = LoadOrCreateManifest(root, mapName);
        var info = manifest.GetOrCreateChunk(coord);
        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
        if (!File.Exists(paintedPath) && info.Source != ChunkPaintSource.UserPainted)
        {
            return Fail($"Chunk ({coord.X},{coord.Y}) has no user painted data.");
        }

        RevertChunkToGenerated(root, mapName, manifest, info, coord);
        AssetDatabase.SaveAssets();
        MapPaintBackgroundPreview.TryAutoSync(root, mapName);

        return new ExportResult
        {
            Success = true,
            Message = $"Cleared user painted for chunk ({coord.X},{coord.Y}).",
            Manifest = manifest,
        };
    }

    public static ExportResult RecaptureChunkTemplate(MapChunkEditorRoot root, string mapName, ChunkCoord coord)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        var manifest = LoadOrCreateManifest(root, mapName);
        var settings = MapChunkEditorSettings.GetOrCreate();
        float paintPpu = settings.EffectivePaintExportPpu;
        var info = manifest.GetOrCreateChunk(coord);
        if (!UpdateChunkTemplate(root, mapName, manifest, info, coord, paintPpu, forceCapture: true))
        {
            return Fail($"Failed to re-capture template for chunk ({coord.X},{coord.Y}).");
        }

        info.TemplateStale = false;
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();

        return new ExportResult
        {
            Success = true,
            Message = $"Re-captured scene template for chunk ({coord.X},{coord.Y}).",
            Manifest = manifest,
        };
    }

    public static ExportResult ApplyToDatabase(MapChunkEditorRoot root, string mapName)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (root.PaintWorldRect.width <= 0f || root.PaintWorldRect.height <= 0f)
        {
            return Fail("PaintWorldRect is not configured.");
        }

        string dbPath = $"Assets/Resources/MapChunk/{mapName}.asset";
        var database = AssetDatabase.LoadAssetAtPath<MapChunkDatabase>(dbPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<MapChunkDatabase>();
            database.AreaId = mapName;
            database.SceneName = mapName;
            database.Chunks = new List<MapChunkExportItem>();
            AssetDatabase.CreateAsset(database, dbPath);
        }

        var settings = MapChunkEditorSettings.GetOrCreate();
        database.ChunkWorldSize = root.ChunkWorldSize;
        database.TexturePPU = settings.TexturePPU;
        database.ChunkOrigin = root.ChunkOrigin;
        database.SourceTextureWidth = Mathf.Max(database.SourceTextureWidth, settings.PaintSlicePixelSize);
        database.SourceTextureHeight = Mathf.Max(database.SourceTextureHeight, settings.PaintSlicePixelSize);

        database.LogicWorldRect = root.PaintWorldRect;

        var paintCoords = new HashSet<ChunkCoord>();
        MapPaintBackgroundShared.CollectPaintRectCoords(root, paintCoords);
        var lookup = database.Chunks?.ToDictionary(c => (c.X, c.Y), c => c) ?? new Dictionary<(int, int), MapChunkExportItem>();

        // 移除逻辑范围外的历史 chunk 条目
        if (database.Chunks != null && database.Chunks.Count > 0)
        {
            database.Chunks.RemoveAll(c =>
                c != null && !MapChunkUtility.IsChunkInsideWorldRect(
                    new ChunkCoord(c.X, c.Y),
                    root.PaintWorldRect,
                    root.ChunkOrigin,
                    root.ChunkWorldSize));
            lookup = database.Chunks.ToDictionary(c => (c.X, c.Y), c => c);
        }

        foreach (var coord in paintCoords)
        {
            string prefabPath = $"{MapPaintBackgroundShared.GetMapRootFolder(mapName)}/Prefabs/bg_{coord.X}_{coord.Y}.prefab";
            if (!File.Exists(prefabPath))
            {
                continue;
            }

            if (!lookup.TryGetValue((coord.X, coord.Y), out var item))
            {
                item = new MapChunkExportItem { X = coord.X, Y = coord.Y };
                database.Chunks.Add(item);
                lookup[(coord.X, coord.Y)] = item;
            }

            item.BackgroundKey = $"MapChunk/{mapName}/Prefabs/bg_{coord.X}_{coord.Y}";
        }

        database.InvalidateLookup();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        return new ExportResult
        {
            Success = true,
            Message = $"Updated MapChunkDatabase with {paintCoords.Count} paint rect chunk(s).",
            Manifest = null,
        };
    }

    static ExportResult Fail(string message) => new ExportResult { Success = false, Message = message };

    static void SaveManifestState(
        MapChunkEditorRoot root,
        MapPaintManifest manifest,
        string mapName,
        int slicePx,
        float paintPpu,
        float expandRatio)
    {
        var settings = MapChunkEditorSettings.GetOrCreate();
        manifest.ExportRevision++;
        manifest.PaintWorldRect = root.PaintWorldRect;
        manifest.ChunkOrigin = root.ChunkOrigin;
        manifest.ChunkWorldSize = root.ChunkWorldSize;
        manifest.TexturePPU = settings.TexturePPU;
        manifest.PaintExportPPU = paintPpu;
        manifest.SlicePixelSize = slicePx;
        manifest.MaskColor = settings.PaintMaskColor;
        manifest.ContextExpandRatio = expandRatio;
        manifest.InvalidateLookup();

        EditorUtility.SetDirty(manifest);
        root.LastPaintManifestKey = $"MapChunk/{mapName}/PaintExport/{MapPaintBackgroundShared.ManifestFileName}";
        EditorUtility.SetDirty(root);
    }

    static MapPaintManifest LoadOrCreateManifest(MapChunkEditorRoot root, string mapName)
    {
        string path = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(path);
        if (manifest != null)
        {
            return manifest;
        }

        var settings = MapChunkEditorSettings.GetOrCreate();
        manifest = ScriptableObject.CreateInstance<MapPaintManifest>();
        manifest.SceneName = mapName;
        manifest.ChunkWorldSize = root.ChunkWorldSize;
        manifest.ChunkOrigin = root.ChunkOrigin;
        manifest.TexturePPU = settings.TexturePPU;
        manifest.PaintExportPPU = settings.EffectivePaintExportPpu;
        manifest.MaskColor = settings.PaintMaskColor;
        AssetDatabase.CreateAsset(manifest, path);
        return manifest;
    }

    static void RevertChunkToGenerated(
        MapChunkEditorRoot root,
        string mapName,
        MapPaintManifest manifest,
        MapPaintChunkInfo info,
        ChunkCoord coord)
    {
        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
        if (File.Exists(paintedPath))
        {
            AssetDatabase.DeleteAsset(paintedPath);
        }

        info.Source = ChunkPaintSource.Generated;

        string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);
        var settings = MapChunkEditorSettings.GetOrCreate();
        float ppu = settings.TexturePPU > 0f ? settings.TexturePPU : settings.EffectivePaintExportPpu;
        string templatePath = MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);
        var templateTex = MapPaintBackgroundShared.LoadTextureFromAssetPath(templatePath);
        if (templateTex == null)
        {
            return;
        }

        int bgSlice = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, ppu);
        var bgTex = MapPaintBackgroundShared.ResampleTexture(templateTex, bgSlice, bgSlice, FilterMode.Bilinear);
        Object.DestroyImmediate(templateTex);
        string spritePath = MapPaintBackgroundShared.SaveBackgroundSprite(bgTex, coord, ppu, rootFolder);
        Object.DestroyImmediate(bgTex);
        if (!string.IsNullOrEmpty(spritePath))
        {
            MapPaintBackgroundShared.SaveBackgroundPrefab(coord, spritePath, rootFolder, settings.BackgroundSortingOrder);
            AssetDatabase.ImportAsset(spritePath);
        }

        EditorUtility.SetDirty(manifest);
    }

    static bool UpdateChunkTemplate(
        MapChunkEditorRoot root,
        string mapName,
        MapPaintManifest manifest,
        MapPaintChunkInfo info,
        ChunkCoord coord,
        float paintPpu,
        bool forceCapture)
    {
        string templatePath = MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);
        if (!forceCapture && File.Exists(templatePath))
        {
            return false;
        }

        var tex = MapPaintBackgroundCapture.CaptureChunk(
            root,
            coord,
            paintPpu,
            MapChunkEditorSettings.GetOrCreate().PaintMaskColor,
            out var coverage);
        if (tex == null)
        {
            return false;
        }

        info.TileCoverageRatio = coverage;
        MapPaintBackgroundShared.WritePng(tex, templatePath);
        AssetDatabase.ImportAsset(templatePath);
        MapPaintBackgroundShared.ConfigureCaptureTextureImporter(templatePath);
        Object.DestroyImmediate(tex);
        return true;
    }
}
#endif
