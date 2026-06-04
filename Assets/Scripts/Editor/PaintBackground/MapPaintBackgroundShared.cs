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
    public const string AtlasFileName = "atlas_for_ai.png";
    public const string ManifestFileName = "manifest.asset";

    // PaintExport/chunks/chunk_{x}_{y}.png   — AI 参考模板（Camera 拍摄 + Magenta 留白）
    // PaintExport/chunks/painted_{x}_{y}.png — 用户/AI 回稿（Import 写入，不覆盖 chunk_*）
    // PaintExport/export_ai/chunk_{x}_{y}_for_ai.png — 单块外扩后给 AI 的 PNG（Import 时需裁切）

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

    public static string GetAtlasPath(string mapName) =>
        $"{GetPaintExportFolder(mapName)}/{AtlasFileName}";

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

        MapChunkUtility.CollectChunkCoordsForWorldRect(
            root.PaintWorldRect,
            root.ChunkOrigin,
            root.ChunkWorldSize,
            output);
    }

    public static Texture2D LoadTextureFromAssetPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(assetPath);
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
