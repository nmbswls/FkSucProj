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

    public static ExportResult GenerateAtlas(MapChunkEditorRoot root, string mapName, FilterMode resampleFilter = FilterMode.Bilinear)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (string.IsNullOrWhiteSpace(mapName))
        {
            return Fail("Map name is empty.");
        }

        if (root.PaintWorldRect.width <= 0f || root.PaintWorldRect.height <= 0f)
        {
            return Fail("PaintWorldRect is not configured.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        var manifest = LoadOrCreateManifest(root, mapName);

        float paintPpu = root.EffectivePaintExportPpu;
        int slicePx = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, paintPpu);
        var layout = ComputeAtlasLayout(root.PaintWorldRect, root.ChunkOrigin, root.ChunkWorldSize, slicePx);

        var atlas = new Texture2D(layout.AtlasWidth, layout.AtlasHeight, TextureFormat.RGBA32, false);
        var clear = new Color[layout.AtlasWidth * layout.AtlasHeight];
        for (int i = 0; i < clear.Length; i++)
        {
            clear[i] = root.PaintMaskColor;
        }

        atlas.SetPixels(clear);

        int rasterizedCount = 0;
        int reusedCount = 0;
        string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);

        try
        {
            foreach (var coord in layout.ChunkCoords.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                var info = manifest.GetOrCreateChunk(coord);
                bool templateCaptured = UpdateChunkTemplate(
                    root,
                    mapName,
                    manifest,
                    info,
                    coord,
                    paintPpu,
                    forceCapture: false);

                if (templateCaptured)
                {
                    rasterizedCount++;
                }
                else
                {
                    reusedCount++;
                }

                var chunkTex = LoadAtlasChunkTexture(mapName, info, coord, slicePx, resampleFilter);
                if (chunkTex == null)
                {
                    continue;
                }

                CopyToAtlas(atlas, chunkTex, layout, coord, slicePx);
                Object.DestroyImmediate(chunkTex);

                UpdateBackgroundForChunk(root, mapName, coord, info, paintPpu, rootFolder, slicePx, resampleFilter);

                if (info.ResetOnExport)
                {
                    info.ResetOnExport = false;
                }
            }

            atlas.Apply();
            MapPaintBackgroundShared.WritePng(atlas, MapPaintBackgroundShared.GetAtlasPath(mapName));

            manifest.ExportRevision++;
            manifest.PaintWorldRect = root.PaintWorldRect;
            manifest.ChunkOrigin = root.ChunkOrigin;
            manifest.ChunkWorldSize = root.ChunkWorldSize;
            manifest.TexturePPU = root.TexturePPU;
            manifest.PaintExportPPU = paintPpu;
            manifest.SlicePixelSize = slicePx;
            manifest.AtlasWidth = layout.AtlasWidth;
            manifest.AtlasHeight = layout.AtlasHeight;
            manifest.MaskColor = root.PaintMaskColor;
            manifest.ContextExpandRatio = root.PaintContextExpandRatio;
            manifest.InvalidateLookup();

            EditorUtility.SetDirty(manifest);
            root.LastPaintManifestKey = $"MapChunk/{mapName}/PaintExport/{MapPaintBackgroundShared.ManifestFileName}";
            EditorUtility.SetDirty(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MapPaintBackgroundPreview.TryAutoSync(root, mapName);

            return new ExportResult
            {
                Success = true,
                Message = $"Paint atlas generated: {layout.ChunkCoords.Count} chunk(s), rasterized {rasterizedCount}, reused {reusedCount}.",
                Manifest = manifest,
            };
        }
        finally
        {
            Object.DestroyImmediate(atlas);
        }
    }

    public static ExportResult SyncOutline(
        MapChunkEditorRoot root,
        string mapName,
        IEnumerable<ChunkCoord> targets,
        bool allChunks,
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
        float paintPpu = root.EffectivePaintExportPpu;
        var targetSet = BuildTargetSet(root, targets, allChunks);
        int synced = 0;

        foreach (var coord in targetSet)
        {
            var info = manifest.GetOrCreateChunk(coord);
            UpdateChunkTemplate(
                root,
                mapName,
                manifest,
                info,
                coord,
                paintPpu,
                forceCapture: true);
            synced++;
        }

        manifest.OutlineSyncedRevision = manifest.ExportRevision;
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();

        return new ExportResult
        {
            Success = true,
            Message = $"Synced outline for {synced} chunk(s).",
            Manifest = manifest,
        };
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
        float paintPpu = root.EffectivePaintExportPpu;
        int slicePx = manifest.SlicePixelSize > 0
            ? manifest.SlicePixelSize
            : MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, paintPpu);

        var info = manifest.GetOrCreateChunk(coord);
        UpdateChunkTemplate(root, mapName, manifest, info, coord, paintPpu, forceCapture: info.ResetOnExport);
        if (info.ResetOnExport)
        {
            info.ResetOnExport = false;
        }

        float expandRatio = root.PaintContextExpandRatio;
        Texture2D forAi = null;
        try
        {
            forAi = MapPaintBackgroundContext.BuildChunkForAi(
                mapName,
                manifest,
                coord,
                slicePx,
                expandRatio,
                root.PaintMaskColor,
                resampleFilter);

            string outputPath = MapPaintBackgroundShared.GetChunkForAiPath(mapName, coord);
            MapPaintBackgroundShared.WritePng(forAi, outputPath);
            AssetDatabase.ImportAsset(outputPath);

            manifest.SlicePixelSize = slicePx;
            manifest.PaintExportPPU = paintPpu;
            manifest.ContextExpandRatio = expandRatio;
            manifest.ExportRevision++;
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();

            int contextSize = MapPaintBackgroundContext.ComputeContextSize(slicePx, expandRatio);
            int margin = MapPaintBackgroundContext.ComputeMarginPx(slicePx, expandRatio);
            return new ExportResult
            {
                Success = true,
                Message =
                    $"Exported chunk ({coord.X},{coord.Y}) for AI: {contextSize}x{contextSize}px " +
                    $"(center {slicePx}px + margin {margin}px). Neighbors prefer painted_*.",
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

    public static ExportResult ClearPainted(
        MapChunkEditorRoot root,
        string mapName,
        IEnumerable<ChunkCoord> targets,
        bool allChunks)
    {
        if (root == null)
        {
            return Fail("MapChunkEditorRoot is missing.");
        }

        if (!EditorUtility.DisplayDialog(
                "Clear Painted",
                "Delete painted chunk PNGs and revert bg_* to chunk templates?",
                "Clear",
                "Cancel"))
        {
            return Fail("Cancelled.");
        }

        MapPaintBackgroundShared.EnsurePaintFolders(mapName);
        var manifest = LoadOrCreateManifest(root, mapName);
        var targetSet = BuildTargetSet(root, targets, allChunks);
        string rootFolder = MapPaintBackgroundShared.GetMapRootFolder(mapName);
        float ppu = root.TexturePPU > 0f ? root.TexturePPU : root.EffectivePaintExportPpu;
        int cleared = 0;

        foreach (var coord in targetSet)
        {
            var info = manifest.GetOrCreateChunk(coord);
            string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
            if (File.Exists(paintedPath))
            {
                AssetDatabase.DeleteAsset(paintedPath);
            }

            info.Source = ChunkPaintSource.Generated;
            info.ResetOnExport = false;

            var templatePath = MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);
            var templateTex = MapPaintBackgroundShared.LoadTextureFromAssetPath(templatePath);
            if (templateTex != null)
            {
                int bgSlice = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, ppu);
                var bgTex = MapPaintBackgroundShared.ResampleTexture(templateTex, bgSlice, bgSlice, FilterMode.Bilinear);
                Object.DestroyImmediate(templateTex);
                string spritePath = MapPaintBackgroundShared.SaveBackgroundSprite(bgTex, coord, ppu, rootFolder);
                Object.DestroyImmediate(bgTex);
                if (!string.IsNullOrEmpty(spritePath))
                {
                    MapPaintBackgroundShared.SaveBackgroundPrefab(
                        coord,
                        spritePath,
                        rootFolder,
                        root.BackgroundSortingOrder);
                    AssetDatabase.ImportAsset(spritePath);
                }
            }

            cleared++;
        }

        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        MapPaintBackgroundPreview.TryAutoSync(root, mapName);

        return new ExportResult
        {
            Success = true,
            Message = $"Cleared painted data for {cleared} chunk(s).",
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

        database.ChunkWorldSize = root.ChunkWorldSize;
        database.TexturePPU = root.TexturePPU;
        database.ChunkOrigin = root.ChunkOrigin;
        database.SourceTextureWidth = Mathf.Max(database.SourceTextureWidth, root.PaintSlicePixelSize);
        database.SourceTextureHeight = Mathf.Max(database.SourceTextureHeight, root.PaintSlicePixelSize);

        var paintCoords = new HashSet<ChunkCoord>();
        MapPaintBackgroundShared.CollectPaintRectCoords(root, paintCoords);
        var lookup = database.Chunks?.ToDictionary(c => (c.X, c.Y), c => c) ?? new Dictionary<(int, int), MapChunkExportItem>();

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

    static MapPaintManifest LoadOrCreateManifest(MapChunkEditorRoot root, string mapName)
    {
        string path = MapPaintBackgroundShared.GetManifestPath(mapName);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(path);
        if (manifest != null)
        {
            return manifest;
        }

        manifest = ScriptableObject.CreateInstance<MapPaintManifest>();
        manifest.SceneName = mapName;
        manifest.ChunkWorldSize = root.ChunkWorldSize;
        manifest.ChunkOrigin = root.ChunkOrigin;
        manifest.TexturePPU = root.TexturePPU;
        manifest.PaintExportPPU = root.EffectivePaintExportPpu;
        manifest.MaskColor = root.PaintMaskColor;
        AssetDatabase.CreateAsset(manifest, path);
        return manifest;
    }

    struct AtlasLayout
    {
        public ChunkCoord MinCoord;
        public int Cols;
        public int Rows;
        public int AtlasWidth;
        public int AtlasHeight;
        public List<ChunkCoord> ChunkCoords;
    }

    static AtlasLayout ComputeAtlasLayout(Rect paintRect, Vector2 origin, float chunkSize, int slicePx)
    {
        var minCoord = MapChunkUtility.WorldToChunk(new Vector2(paintRect.xMin, paintRect.yMin), origin, chunkSize);
        var maxCoord = MapChunkUtility.WorldToChunk(
            new Vector2(paintRect.xMax - 1e-4f, paintRect.yMax - 1e-4f),
            origin,
            chunkSize);

        int cols = maxCoord.X - minCoord.X + 1;
        int rows = maxCoord.Y - minCoord.Y + 1;
        var coords = new List<ChunkCoord>();
        for (int cy = minCoord.Y; cy <= maxCoord.Y; cy++)
        {
            for (int cx = minCoord.X; cx <= maxCoord.X; cx++)
            {
                coords.Add(new ChunkCoord(cx, cy));
            }
        }

        return new AtlasLayout
        {
            MinCoord = minCoord,
            Cols = cols,
            Rows = rows,
            AtlasWidth = cols * slicePx,
            AtlasHeight = rows * slicePx,
            ChunkCoords = coords,
        };
    }

    static HashSet<ChunkCoord> BuildTargetSet(MapChunkEditorRoot root, IEnumerable<ChunkCoord> targets, bool allChunks)
    {
        var set = new HashSet<ChunkCoord>();
        if (allChunks)
        {
            MapPaintBackgroundShared.CollectPaintRectCoords(root, set);
            return set;
        }

        if (targets != null)
        {
            foreach (var coord in targets)
            {
                set.Add(coord);
            }
        }

        return set;
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
        bool shouldCapture = forceCapture || info.ResetOnExport || !File.Exists(templatePath);
        if (!shouldCapture)
        {
            return false;
        }

        var tex = MapPaintBackgroundCapture.CaptureChunk(
            root,
            coord,
            paintPpu,
            root.PaintMaskColor,
            out var coverage);
        if (tex == null)
        {
            return false;
        }

        info.TileCoverageRatio = coverage;
        MapPaintBackgroundShared.WritePng(tex, templatePath);
        AssetDatabase.ImportAsset(templatePath);
        Object.DestroyImmediate(tex);

        if (forceCapture || info.ResetOnExport)
        {
            manifest.OutlineSyncedRevision = manifest.ExportRevision;
        }

        return true;
    }

    static Texture2D LoadAtlasChunkTexture(
        string mapName,
        MapPaintChunkInfo info,
        ChunkCoord coord,
        int slicePx,
        FilterMode resampleFilter)
    {
        string templatePath = MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);
        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);

        if (info.Source == ChunkPaintSource.UserPainted && File.Exists(paintedPath))
        {
            var painted = MapPaintBackgroundShared.LoadTextureFromAssetPath(paintedPath);
            return EnsureSize(painted, slicePx, resampleFilter);
        }

        var existing = MapPaintBackgroundShared.LoadTextureFromAssetPath(templatePath);
        return EnsureSize(existing, slicePx, resampleFilter);
    }

    static Texture2D EnsureSize(Texture2D tex, int slicePx, FilterMode filter)
    {
        if (tex == null)
        {
            return null;
        }

        if (tex.width == slicePx && tex.height == slicePx)
        {
            return tex;
        }

        var resampled = MapPaintBackgroundShared.ResampleTexture(tex, slicePx, slicePx, filter);
        Object.DestroyImmediate(tex);
        return resampled;
    }

    static void CopyToAtlas(Texture2D atlas, Texture2D chunkTex, AtlasLayout layout, ChunkCoord coord, int slicePx)
    {
        int col = coord.X - layout.MinCoord.X;
        int row = coord.Y - layout.MinCoord.Y;
        int dstX = col * slicePx;
        int dstY = row * slicePx;
        atlas.SetPixels(dstX, dstY, slicePx, slicePx, chunkTex.GetPixels());
    }

    static void UpdateBackgroundForChunk(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        MapPaintChunkInfo info,
        float paintPpu,
        string rootFolder,
        int slicePx,
        FilterMode resampleFilter)
    {
        float bgPpu = root.TexturePPU > 0f ? root.TexturePPU : paintPpu;
        int bgSlice = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, bgPpu);

        string sourcePath = info.Source == ChunkPaintSource.UserPainted &&
                            File.Exists(MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord))
            ? MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord)
            : MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);

        var src = MapPaintBackgroundShared.LoadTextureFromAssetPath(sourcePath);
        if (src == null)
        {
            return;
        }

        var bgTex = MapPaintBackgroundShared.ResampleTexture(src, bgSlice, bgSlice, resampleFilter);
        Object.DestroyImmediate(src);

        string spritePath = MapPaintBackgroundShared.SaveBackgroundSprite(bgTex, coord, bgPpu, rootFolder);
        Object.DestroyImmediate(bgTex);
        if (string.IsNullOrEmpty(spritePath))
        {
            return;
        }

        MapPaintBackgroundShared.SaveBackgroundPrefab(coord, spritePath, rootFolder, root.BackgroundSortingOrder);
        AssetDatabase.ImportAsset(spritePath);
    }
}
#endif
