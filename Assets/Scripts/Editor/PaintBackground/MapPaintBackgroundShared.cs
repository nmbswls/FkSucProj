#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEngine;

// 手绘背景管线：资源路径与 bg sprite/prefab 写入
public static class MapPaintBackgroundShared
{
    public const string PaintExportFolderName = "PaintExport";
    public const string ChunksFolderName = "chunks";
    public const string ExportAiFolderName = "export_ai";
    public const string ManifestFileName = "manifest.asset";

    // chunks/chunk_* — 模板；chunks/painted_* — 回稿；export_ai/chunk_*_for_ai — 给 AI 的外扩图

    public static string GetMapRootFolder(string mapName) => $"Assets/Resources/MapChunk/{mapName}";

    public static string GetPaintExportFolder(string mapName) =>
        $"{GetMapRootFolder(mapName)}/{PaintExportFolderName}";

    public static string GetChunksFolder(string mapName) =>
        $"{GetPaintExportFolder(mapName)}/{ChunksFolderName}";

    public static string GetChunkTemplatePath(string mapName, ChunkCoord coord) =>
        $"{GetChunksFolder(mapName)}/chunk_{coord.X}_{coord.Y}.png";

    public static string GetPaintedChunkPath(string mapName, ChunkCoord coord) =>
        $"{GetChunksFolder(mapName)}/painted_{coord.X}_{coord.Y}.png";

    public static string GetExportAiFolder(string mapName) =>
        $"{GetPaintExportFolder(mapName)}/{ExportAiFolderName}";

    public static string GetChunkForAiPath(string mapName, ChunkCoord coord) =>
        $"{GetExportAiFolder(mapName)}/chunk_{coord.X}_{coord.Y}_for_ai.png";

    public static string GetChunkPaintedRefPath(string mapName, ChunkCoord coord) =>
        $"{GetExportAiFolder(mapName)}/chunk_{coord.X}_{coord.Y}_painted_ref.png";

    public static string GetManifestPath(string mapName) =>
        $"{GetPaintExportFolder(mapName)}/{ManifestFileName}";

    public static string GetBackupFolder(string mapName, int revision) =>
        $"{GetPaintExportFolder(mapName)}/backup_rev{revision}";

    public static void EnsurePaintFolders(string mapName)
    {
        MapChunkExportCore.EnsureFolderPublic("Assets/Resources");
        MapChunkExportCore.EnsureFolderPublic("Assets/Resources/MapChunk");
        MapChunkExportCore.EnsureFolderPublic(GetMapRootFolder(mapName));
        MapChunkExportCore.EnsureFolderPublic($"{GetMapRootFolder(mapName)}/Sprites");
        MapChunkExportCore.EnsureFolderPublic($"{GetMapRootFolder(mapName)}/Prefabs");
        MapChunkExportCore.EnsureFolderPublic(GetPaintExportFolder(mapName));
        MapChunkExportCore.EnsureFolderPublic(GetChunksFolder(mapName));
        MapChunkExportCore.EnsureFolderPublic(GetExportAiFolder(mapName));
    }

    public static void CollectPaintRectCoords(MapChunkEditorRoot root, HashSet<ChunkCoord> output)
    {
        if (root == null || output == null)
        {
            return;
        }

        if (root.PaintWorldRect.width <= 0f || root.PaintWorldRect.height <= 0f)
        {
            return;
        }

        MapChunkUtility.CollectChunkCoordsForWorldRect(
            root.PaintWorldRect,
            root.ChunkOrigin,
            root.ChunkWorldSize,
            output);

        output.RemoveWhere(c => !MapChunkUtility.IsChunkInsideWorldRect(
            c,
            root.PaintWorldRect,
            root.ChunkOrigin,
            root.ChunkWorldSize));
    }

    public static Texture2D LoadTextureFromAssetPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
        {
            return null;
        }

        return LoadTextureFromBytes(File.ReadAllBytes(assetPath));
    }

    public static Texture2D LoadTextureFromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.DestroyImmediate(tex);
            return null;
        }

        return tex;
    }

    public static Texture2D ResampleTexture(Texture2D source, int targetW, int targetH, FilterMode filterMode)
    {
        if (source == null)
        {
            return null;
        }

        if (source.width == targetW && source.height == targetH)
        {
            return DuplicateTexture(source);
        }

        var rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = filterMode;
        var prev = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        var dst = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        dst.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        dst.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }

    public static Texture2D DuplicateTexture(Texture2D source)
    {
        var dst = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        dst.SetPixels(source.GetPixels());
        dst.Apply();
        return dst;
    }

    public static void WritePng(Texture2D tex, string assetPath)
    {
        if (tex == null)
        {
            return;
        }

        var dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
    }

    public static string SaveBackgroundSprite(Texture2D chunkTex, ChunkCoord coord, float ppu, string rootFolder)
    {
        if (chunkTex == null)
        {
            return null;
        }

        string spritePath = $"{rootFolder}/Sprites/bg_{coord.X}_{coord.Y}.png";
        WritePng(chunkTex, spritePath);
        AssetDatabase.ImportAsset(spritePath);
        ConfigureBackgroundSpriteImporter(spritePath, ppu);
        return spritePath;
    }

    public static string SaveBackgroundPrefab(ChunkCoord coord, string spriteAssetPath, string rootFolder, int sortingOrder)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
        if (sprite == null)
        {
            return null;
        }

        var go = new GameObject($"bg_{coord.X}_{coord.Y}");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        var boundsMin = sprite.bounds.min;
        go.transform.localPosition = new Vector3(-boundsMin.x, -boundsMin.y, 0f);

        string prefabPath = $"{rootFolder}/Prefabs/bg_{coord.X}_{coord.Y}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefabPath;
    }

    public static void ConfigureCaptureTextureImporter(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // painted/template → 裁剪（如需）→ 写入 bg sprite/prefab
    public static bool PackRuntimeBackgroundFromPainted(
        MapChunkEditorRoot root,
        string mapName,
        ChunkCoord coord,
        MapPaintManifest manifest,
        FilterMode resampleFilter = FilterMode.Bilinear)
    {
        if (root == null)
        {
            return false;
        }

        string sourcePath = GetPaintedChunkPath(mapName, coord);
        if (!File.Exists(sourcePath))
        {
            sourcePath = GetChunkTemplatePath(mapName, coord);
        }

        if (!File.Exists(sourcePath))
        {
            return false;
        }

        var settings = MapChunkEditorSettings.GetOrCreate();
        float bgPpu = settings.TexturePPU > 0f ? settings.TexturePPU : settings.EffectivePaintExportPpu;
        int slicePx = manifest != null && manifest.SlicePixelSize > 0
            ? manifest.SlicePixelSize
            : MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, bgPpu);
        float expandRatio = manifest != null && manifest.ContextExpandRatio > 0f
            ? manifest.ContextExpandRatio
            : settings.PaintContextExpandRatio;
        int contextSize = MapPaintBackgroundContext.ComputeContextSize(slicePx, expandRatio);
        int bgSlice = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, bgPpu);
        string rootFolder = GetMapRootFolder(mapName);

        var src = LoadTextureFromAssetPath(sourcePath);
        if (src == null)
        {
            return false;
        }

        Texture2D sliceTex = null;
        Texture2D bgTex = null;
        try
        {
            if (src.width == contextSize && src.height == contextSize && contextSize != slicePx)
            {
                sliceTex = MapPaintBackgroundContext.CropCenterFromContext(
                    src, slicePx, expandRatio, resampleFilter);
            }
            else if (src.width == slicePx && src.height == slicePx)
            {
                sliceTex = DuplicateTexture(src);
            }
            else
            {
                sliceTex = ResampleTexture(src, slicePx, slicePx, resampleFilter);
            }

            if (sliceTex == null)
            {
                return false;
            }

            bgTex = ResampleTexture(sliceTex, bgSlice, bgSlice, resampleFilter);
            if (bgTex == null)
            {
                return false;
            }

            string spritePath = SaveBackgroundSprite(bgTex, coord, bgPpu, rootFolder);
            if (string.IsNullOrEmpty(spritePath))
            {
                return false;
            }

            SaveBackgroundPrefab(coord, spritePath, rootFolder, settings.BackgroundSortingOrder);
            AssetDatabase.ImportAsset(spritePath);
            return true;
        }
        finally
        {
            if (src != null)
            {
                Object.DestroyImmediate(src);
            }

            if (sliceTex != null)
            {
                Object.DestroyImmediate(sliceTex);
            }

            if (bgTex != null)
            {
                Object.DestroyImmediate(bgTex);
            }
        }
    }

    public static string BuildRuntimeBackgroundKey(string mapName, ChunkCoord coord) =>
        $"MapChunk/{mapName}/Prefabs/bg_{coord.X}_{coord.Y}";

    public static void ConfigureBackgroundSpriteImporter(string spritePath, float ppu)
    {
        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
        settings.spritePivot = Vector2.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
#endif
