using System.Collections.Generic;
using System.IO;
using System.Linq;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapChunkExportCore
{
    public struct ExportResult
    {
        public bool Success;
        public string Message;
        public MapChunkDatabase Database;
        public int BackgroundChunkCount;
        public int TilemapChunkCount;
        public bool GridRootPrefabExported;
    }

    public static ExportResult Export(
        MapChunkEditorRoot editorRoot,
        string mapName,
        int backgroundSortingOrder,
        float chunkWorldSize,
        Vector2 chunkOrigin,
        bool exportBackground,
        bool exportTilemap,
        bool exportGridRootPrefab,
        bool bakeVisualLayers = true)
    {
        if (editorRoot == null)
        {
            return Fail("MapChunkEditorRoot is missing on AreaRoot.");
        }

        if (string.IsNullOrWhiteSpace(mapName))
        {
            return Fail("Map name is empty.");
        }

        if (exportBackground && editorRoot.SourceTexture == null)
        {
            return Fail("Assign SourceTexture on MapChunkEditorRoot for background export.");
        }

        MapChunkEditorTilemapResolver.TryResolveTileGrounds(editorRoot, out var tileGrounds);
        if (exportTilemap && (tileGrounds == null || tileGrounds.Length == 0))
        {
            return Fail("GridRoot/Tilemap not found under StaticPrefabRoot. Create or import Grid before tilemap export.");
        }

        editorRoot.ChunkWorldSize = chunkWorldSize;
        editorRoot.ChunkOrigin = chunkOrigin;
        EditorUtility.SetDirty(editorRoot);

        var texSize = editorRoot.SourceTextureSize;
        int slicePx = editorRoot.SlicePixelSize;
        float chunkSize = editorRoot.ChunkWorldSize;
        float ppu = editorRoot.TexturePPU;
        var origin = editorRoot.ChunkOrigin;

        Texture2D fullSourceTexture = null;
        try
        {
            fullSourceTexture = LoadFullSourceTexture(editorRoot.SourceTexture);
            if (fullSourceTexture != null && exportBackground)
            {
                texSize = new Vector2Int(fullSourceTexture.width, fullSourceTexture.height);
                var imported = editorRoot.ImportedTextureSize;
                if (imported.x != texSize.x || imported.y != texSize.y)
                {
                    Debug.LogWarning(
                        $"[MapChunkExport] Source texture {texSize.x}x{texSize.y}px, " +
                        $"imported asset {imported.x}x{imported.y}px (maxTextureSize). Export uses source file size.");
                }
            }
            else if (exportBackground && fullSourceTexture == null)
            {
                Debug.LogWarning("[MapChunkExport] Failed to load source file; fallback to imported texture size.");
            }

            string rootFolder = $"Assets/Resources/MapChunk/{mapName}";
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/MapChunk");
        EnsureFolder(rootFolder);
        EnsureFolder($"{rootFolder}/Sprites");
        EnsureFolder($"{rootFolder}/Prefabs");

        var database = ScriptableObject.CreateInstance<MapChunkDatabase>();
        database.AreaId = mapName;
        database.SceneName = mapName;
        database.ChunkWorldSize = chunkSize;
        database.TexturePPU = ppu;
        database.ChunkOrigin = origin;
        database.SourceTextureWidth = texSize.x;
        database.SourceTextureHeight = texSize.y;
        database.Chunks = new List<MapChunkExportItem>();

        var chunkCoords = CollectAllChunkCoords(texSize, slicePx, tileGrounds, chunkSize, origin, exportBackground, exportTilemap, null, editorRoot);
        int bgCount = 0;
        int tmCount = 0;

        List<MapChunkVisualBaker.BakedLayer> bakedVisualLayers = null;
        if (exportTilemap && bakeVisualLayers)
        {
            EnsureFolder($"{rootFolder}/BakedTiles");
            bakedVisualLayers = MapChunkVisualBaker.Bake(editorRoot, $"{rootFolder}/BakedTiles", tileGrounds);
            chunkCoords = CollectAllChunkCoords(texSize, slicePx, tileGrounds, chunkSize, origin, exportBackground, exportTilemap, bakedVisualLayers, editorRoot);
        }

        foreach (var coord in chunkCoords.OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            var item = new MapChunkExportItem
            {
                X = coord.X,
                Y = coord.Y
            };

            if (exportBackground && editorRoot.SourceTexture != null)
            {
                var crop = MapChunkUtility.TextureCropRect(coord, slicePx, texSize);
                if (crop.width > 0f && crop.height > 0f)
                {
                    var sourceForCrop = fullSourceTexture != null ? fullSourceTexture : editorRoot.SourceTexture;
                    string bgSpritePath = ExportBackgroundSprite(sourceForCrop, coord, crop, slicePx, ppu, rootFolder);
                    if (!string.IsNullOrEmpty(bgSpritePath))
                    {
                        string bgPrefabPath = CreateBackgroundPrefab(coord, bgSpritePath, rootFolder, backgroundSortingOrder);
                        item.BackgroundKey = $"MapChunk/{mapName}/Prefabs/bg_{coord.X}_{coord.Y}";
                        AssetDatabase.ImportAsset(bgPrefabPath);
                        bgCount++;
                    }
                }
            }

            if (exportTilemap)
            {
                string tmPrefabPath = ExportTilemapChunk(coord, chunkSize, origin, tileGrounds, bakedVisualLayers, rootFolder);
                if (!string.IsNullOrEmpty(tmPrefabPath))
                {
                    item.TilemapKey = $"MapChunk/{mapName}/Prefabs/tm_{coord.X}_{coord.Y}";
                    AssetDatabase.ImportAsset(tmPrefabPath);
                    tmCount++;
                }
            }

            if (!string.IsNullOrEmpty(item.BackgroundKey) || !string.IsNullOrEmpty(item.TilemapKey))
            {
                database.Chunks.Add(item);
            }
        }

        bool gridRootExported = false;
        if (exportGridRootPrefab)
        {
            gridRootExported = ExportGridRootPrefab(editorRoot, rootFolder);
            if (gridRootExported)
            {
                database.WalkGridKey = $"MapChunk/{mapName}/Prefabs/GridRoot";
            }
        }

        string dbPath = $"Assets/Resources/MapChunk/{mapName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<MapChunkDatabase>(dbPath);
        if (existing != null)
        {
            existing.AreaId = database.AreaId;
            existing.SceneName = database.SceneName;
            existing.ChunkWorldSize = database.ChunkWorldSize;
            existing.TexturePPU = database.TexturePPU;
            existing.ChunkOrigin = database.ChunkOrigin;
            existing.SourceTextureWidth = database.SourceTextureWidth;
            existing.SourceTextureHeight = database.SourceTextureHeight;
            if (exportGridRootPrefab && gridRootExported)
            {
                existing.WalkGridKey = database.WalkGridKey;
            }
            existing.Chunks = database.Chunks;
            existing.InvalidateLookup();
            EditorUtility.SetDirty(existing);
            database = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(database, dbPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new ExportResult
        {
            Success = true,
            Message = BuildSuccessMessage(dbPath, bgCount, tmCount, gridRootExported, exportBackground, exportTilemap, exportGridRootPrefab, bakeVisualLayers),
            Database = database,
            BackgroundChunkCount = bgCount,
            TilemapChunkCount = tmCount,
            GridRootPrefabExported = gridRootExported
        };
        }
        finally
        {
            if (fullSourceTexture != null)
            {
                Object.DestroyImmediate(fullSourceTexture);
            }
        }
    }

    static string BuildSuccessMessage(
        string dbPath,
        int bgCount,
        int tmCount,
        bool gridRootExported,
        bool exportBackground,
        bool exportTilemap,
        bool exportGridRootPrefab,
        bool bakeVisualLayers)
    {
        var parts = new List<string> { $"database -> {dbPath}" };
        if (exportBackground)
        {
            parts.Add($"background chunks: {bgCount}");
        }

        if (exportTilemap)
        {
            parts.Add($"tilemap(grid) chunks: {tmCount}");
            if (bakeVisualLayers)
            {
                parts.Add("visual bake: on");
            }
        }

        if (exportGridRootPrefab)
        {
            parts.Add(gridRootExported ? "GridRoot prefab: yes" : "GridRoot prefab: skipped (not found)");
        }

        return string.Join(", ", parts);
    }

    static HashSet<ChunkCoord> CollectAllChunkCoords(
        Vector2Int texSize,
        int slicePx,
        Tilemap[] tileGrounds,
        float chunkSize,
        Vector2 origin,
        bool includeTexture,
        bool includeTilemap,
        List<MapChunkVisualBaker.BakedLayer> bakedVisualLayers,
        MapChunkEditorRoot editorRoot = null)
    {
        var coords = new HashSet<ChunkCoord>();

        if (includeTexture && texSize.x > 0 && texSize.y > 0 && slicePx > 0)
        {
            MapChunkUtility.IterateChunkCoordsForTexture(texSize, slicePx, c => coords.Add(c));
        }

        if (editorRoot != null && editorRoot.PaintWorldRect.width > 0f && editorRoot.PaintWorldRect.height > 0f)
        {
            MapChunkUtility.CollectChunkCoordsForWorldRect(
                editorRoot.PaintWorldRect,
                editorRoot.ChunkOrigin,
                editorRoot.ChunkWorldSize,
                coords);
        }

        if (includeTilemap && tileGrounds != null)
        {
            foreach (var source in tileGrounds)
            {
                if (source == null)
                {
                    continue;
                }

                source.CompressBounds();
                foreach (var pos in source.cellBounds.allPositionsWithin)
                {
                    if (source.GetTile(pos) == null)
                    {
                        continue;
                    }

                    var world = source.GetCellCenterWorld(pos);
                    coords.Add(MapChunkUtility.WorldToChunk(world, origin, chunkSize));
                }
            }
        }

        if (bakedVisualLayers != null)
        {
            foreach (var layer in bakedVisualLayers)
            {
                if (layer?.SourceTilemap == null)
                {
                    continue;
                }

                foreach (var pair in layer.Cells)
                {
                    var world = layer.SourceTilemap.GetCellCenterWorld(pair.Key);
                    coords.Add(MapChunkUtility.WorldToChunk(world, origin, chunkSize));
                }
            }
        }

        return coords;
    }

    static bool ExportGridRootPrefab(MapChunkEditorRoot editorRoot, string rootFolder)
    {
        var gridRoot = MapChunkEditorTilemapResolver.TryGetGridRoot(editorRoot);
        if (gridRoot == null)
        {
            var worldArea = editorRoot.GetComponent<WorldAreaRoot>();
            if (worldArea != null && worldArea.Grid != null)
            {
                gridRoot = worldArea.Grid.transform;
            }
        }

        if (gridRoot == null)
        {
            return false;
        }

        var clone = Object.Instantiate(gridRoot.gameObject);
        clone.name = gridRoot.gameObject.name;
        clone.transform.SetPositionAndRotation(gridRoot.position, gridRoot.rotation);
        clone.transform.localScale = gridRoot.localScale;

        bool export3dCollision = editorRoot != null && editorRoot.ExportGridRoot3DCollision;
        if (export3dCollision)
        {
            float thickness = Mathf.Max(0.01f, editorRoot.GridRootCollisionThickness);
            string layerName = editorRoot.GridRootCollisionLayer;
            int physicsLayer = string.IsNullOrEmpty(layerName) ? -1 : LayerMask.NameToLayer(layerName);
            if (physicsLayer < 0)
            {
                Debug.LogWarning($"[MapChunkExport] GridRoot 3D collision layer '{layerName}' not found, using tilemap layer.");
            }

            var collisionResult = TilemapCollision3DGenerator.GenerateUnderGridRoot(
                clone.transform,
                thickness,
                physicsLayer);

            if (collisionResult.BoxColliderCount > 0)
            {
                Debug.Log(
                    $"[MapChunkExport] GridRoot 3D collision: {collisionResult.TilemapLayerCount} tilemap layer(s), " +
                    $"{collisionResult.BoxColliderCount} merged box(es), thickness={thickness}.");
            }
            else
            {
                Debug.LogWarning(
                    "[MapChunkExport] GridRoot 3D collision enabled but no boxes generated. " +
                    "Ensure tilemap layers have enabled TilemapCollider2D and tiles with collider type.");
            }
        }

        string prefabPath = $"{rootFolder}/Prefabs/GridRoot.prefab";
        PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
        Object.DestroyImmediate(clone);
        AssetDatabase.ImportAsset(prefabPath);

        if (!export3dCollision)
        {
            Debug.Log("[MapChunkExport] GridRoot exported without 3D collision (ExportGridRoot3DCollision is off).");
        }

        return true;
    }

    static ExportResult Fail(string message)
    {
        return new ExportResult { Success = false, Message = message };
    }

    static Texture2D LoadFullSourceTexture(Texture2D asset)
    {
        if (asset == null)
        {
            return null;
        }

        var path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.DestroyImmediate(tex);
            return null;
        }

        return tex;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    public static void EnsureFolderPublic(string path) => EnsureFolder(path);

    static string ExportBackgroundSprite(
        Texture2D src,
        ChunkCoord coord,
        Rect crop,
        int slicePx,
        float ppu,
        string rootFolder)
    {
        var padded = ExtractPaddedRegion(src, crop, slicePx);
        if (padded == null)
        {
            return null;
        }

        string spritePath = $"{rootFolder}/Sprites/bg_{coord.X}_{coord.Y}.png";
        File.WriteAllBytes(spritePath, padded.EncodeToPNG());
        Object.DestroyImmediate(padded);
        AssetDatabase.ImportAsset(spritePath);

        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
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

        return spritePath;
    }

    static Texture2D ExtractPaddedRegion(Texture2D src, Rect crop, int slicePx)
    {
        var full = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, full);
        var prev = RenderTexture.active;
        RenderTexture.active = full;

        var padded = new Texture2D(slicePx, slicePx, TextureFormat.RGBA32, false);
        var clear = new Color[slicePx * slicePx];
        for (int i = 0; i < clear.Length; i++)
        {
            clear[i] = Color.clear;
        }

        padded.SetPixels(clear);

        int w = Mathf.RoundToInt(crop.width);
        int h = Mathf.RoundToInt(crop.height);
        if (w > 0 && h > 0)
        {
            var piece = new Texture2D(w, h, TextureFormat.RGBA32, false);
            piece.ReadPixels(new Rect(crop.x, crop.y, w, h), 0, 0);
            piece.Apply();
            padded.SetPixels(0, 0, w, h, piece.GetPixels());
            Object.DestroyImmediate(piece);
        }

        padded.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(full);
        return padded;
    }

    static string CreateBackgroundPrefab(ChunkCoord coord, string spriteAssetPath, string rootFolder, int sortingOrder)
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
        // prefab 原点 = chunk 左下角；补偿 pivot 不在左下角时的偏移
        var boundsMin = sprite.bounds.min;
        go.transform.localPosition = new Vector3(-boundsMin.x, -boundsMin.y, 0f);

        string prefabPath = $"{rootFolder}/Prefabs/bg_{coord.X}_{coord.Y}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefabPath;
    }

    static string ExportTilemapChunk(
        ChunkCoord coord,
        float chunkSize,
        Vector2 origin,
        Tilemap[] tileGrounds,
        List<MapChunkVisualBaker.BakedLayer> bakedVisualLayers,
        string rootFolder)
    {
        var chunkMin = MapChunkUtility.ChunkWorldMin(coord, origin, chunkSize);
        var chunkMax = chunkMin + new Vector3(chunkSize, chunkSize, 0f);

        var go = new GameObject($"tm_{coord.X}_{coord.Y}");
        bool hasTile = false;

        foreach (var source in tileGrounds)
        {
            if (source == null)
            {
                continue;
            }

            if (TryExportLogicLayer(source, go.transform, chunkMin, chunkMax, ref hasTile))
            {
            }
        }

        if (bakedVisualLayers != null)
        {
            foreach (var baked in bakedVisualLayers)
            {
                if (TryExportBakedVisualLayer(baked, go.transform, chunkMin, chunkMax, ref hasTile))
                {
                }
            }
        }

        if (!hasTile)
        {
            Object.DestroyImmediate(go);
            return null;
        }

        string prefabPath = $"{rootFolder}/Prefabs/tm_{coord.X}_{coord.Y}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefabPath;
    }

    static bool TryExportLogicLayer(Tilemap source, Transform chunkRoot, Vector3 chunkMin, Vector3 chunkMax, ref bool hasTile)
    {
        var layerGo = new GameObject(source.name);
        layerGo.transform.SetParent(chunkRoot, false);
        layerGo.transform.localPosition = source.transform.position - chunkMin;

        var tilemap = layerGo.AddComponent<Tilemap>();
        tilemap.tileAnchor = source.tileAnchor;
        tilemap.orientation = source.orientation;
        var renderer = layerGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = source.GetComponent<TilemapRenderer>()?.sortingOrder ?? 0;

        bool layerHasTile = false;
        foreach (var pos in source.cellBounds.allPositionsWithin)
        {
            var tile = source.GetTile(pos);
            if (tile == null)
            {
                continue;
            }

            var world = source.GetCellCenterWorld(pos);
            if (world.x < chunkMin.x || world.x >= chunkMax.x || world.y < chunkMin.y || world.y >= chunkMax.y)
            {
                continue;
            }

            tilemap.SetTile(pos, tile);
            layerHasTile = true;
        }

        if (!layerHasTile)
        {
            Object.DestroyImmediate(layerGo);
            return false;
        }

        hasTile = true;
        return true;
    }

    static bool TryExportBakedVisualLayer(
        MapChunkVisualBaker.BakedLayer baked,
        Transform chunkRoot,
        Vector3 chunkMin,
        Vector3 chunkMax,
        ref bool hasTile)
    {
        if (baked?.SourceTilemap == null || baked.Cells.Count == 0)
        {
            return false;
        }

        var source = baked.SourceTilemap;
        var layerGo = new GameObject(baked.Name);
        layerGo.transform.SetParent(chunkRoot, false);
        layerGo.transform.localPosition = source.transform.position - chunkMin;

        var tilemap = layerGo.AddComponent<Tilemap>();
        tilemap.tileAnchor = source.tileAnchor;
        tilemap.orientation = source.orientation;
        var renderer = layerGo.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = baked.SortingOrder;

        bool layerHasTile = false;
        foreach (var pair in baked.Cells)
        {
            var world = source.GetCellCenterWorld(pair.Key);
            if (world.x < chunkMin.x || world.x >= chunkMax.x || world.y < chunkMin.y || world.y >= chunkMax.y)
            {
                continue;
            }

            tilemap.SetTile(pair.Key, pair.Value.TileAsset);
            if (pair.Value.Transform != Matrix4x4.identity)
            {
                tilemap.SetTransformMatrix(pair.Key, pair.Value.Transform);
            }

            layerHasTile = true;
        }

        if (!layerHasTile)
        {
            Object.DestroyImmediate(layerGo);
            return false;
        }

        hasTile = true;
        return true;
    }
}
