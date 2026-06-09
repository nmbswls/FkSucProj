using System.Collections.Generic;
using System.IO;
using System.Linq;
using My.Map.Ground;
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
        float chunkWorldSize,
        Vector2 chunkOrigin,
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

        MapChunkEditorTilemapResolver.TryResolveTileGrounds(editorRoot, out var tileGrounds);
        if (exportTilemap && (tileGrounds == null || tileGrounds.Length == 0))
        {
            return Fail("GridRoot/Tilemap not found under StaticPrefabRoot. Create or import Grid before tilemap export.");
        }

        editorRoot.ChunkOrigin = chunkOrigin;
        EditorUtility.SetDirty(editorRoot);

        int slicePx = MapChunkUtility.ComputeSlicePixelSize(chunkWorldSize, MapChunkEditorSettings.GetOrCreate().TexturePPU);
        float chunkSize = chunkWorldSize;
        float ppu = MapChunkEditorSettings.GetOrCreate().TexturePPU;
        var origin = editorRoot.ChunkOrigin;

        string rootFolder = $"Assets/Resources/MapChunk/{mapName}";
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/MapChunk");
        EnsureFolder(rootFolder);
        EnsureFolder($"{rootFolder}/Sprites");
        EnsureFolder($"{rootFolder}/Prefabs");

        var existingDb = AssetDatabase.LoadAssetAtPath<MapChunkDatabase>($"Assets/Resources/MapChunk/{mapName}.asset");
        existingDb?.BuildLookup();

        var database = ScriptableObject.CreateInstance<MapChunkDatabase>();
        database.AreaId = mapName;
        database.SceneName = mapName;
        database.ChunkWorldSize = chunkSize;
        database.TexturePPU = ppu;
        database.ChunkOrigin = origin;
        database.SourceTextureWidth = ComputePaintAtlasWidth(editorRoot, slicePx);
        database.SourceTextureHeight = ComputePaintAtlasHeight(editorRoot, slicePx);
        database.Chunks = new List<MapChunkExportItem>();

        var chunkCoords = CollectAllChunkCoords(tileGrounds, chunkSize, origin, exportTilemap, null, editorRoot);
        int tmCount = 0;

        List<MapChunkVisualBaker.BakedLayer> bakedVisualLayers = null;
        if (exportTilemap && bakeVisualLayers)
        {
            EnsureFolder($"{rootFolder}/BakedTiles");
            bakedVisualLayers = MapChunkVisualBaker.Bake(editorRoot, $"{rootFolder}/BakedTiles", tileGrounds);
            chunkCoords = CollectAllChunkCoords(tileGrounds, chunkSize, origin, exportTilemap, bakedVisualLayers, editorRoot);
        }

        foreach (var coord in chunkCoords.OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            var item = new MapChunkExportItem
            {
                X = coord.X,
                Y = coord.Y
            };

            var existingItem = existingDb?.GetChunkItem(coord);
            if (existingItem != null)
            {
                item.BackgroundKey = existingItem.BackgroundKey;
            }

            if (exportTilemap)
            {
                string tmPrefabPath = ExportTilemapChunk(
                    coord,
                    chunkSize,
                    origin,
                    tileGrounds,
                    bakedVisualLayers,
                    rootFolder,
                    includeLogicLayers: !exportGridRootPrefab);
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
                AssignLogicHeightConfigKey(database, mapName, rootFolder, existingDb);
            }
            else if (existingDb != null && !string.IsNullOrEmpty(existingDb.WalkGridKey))
            {
                database.WalkGridKey = existingDb.WalkGridKey;
            }
        }
        else if (existingDb != null)
        {
            database.WalkGridKey = existingDb.WalkGridKey;
        }

        if (string.IsNullOrEmpty(database.LogicHeightConfigKey))
        {
            AssignLogicHeightConfigKey(database, mapName, rootFolder, existingDb);
        }

        if (editorRoot.PaintWorldRect.width > 0f && editorRoot.PaintWorldRect.height > 0f)
        {
            database.LogicWorldRect = editorRoot.PaintWorldRect;
        }
        else
        {
            database.LogicWorldRect = MapChunkDatabase.ComputeBoundsFromChunks(database.Chunks, origin, chunkSize);
        }

        string dbPath = $"Assets/Resources/MapChunk/{mapName}.asset";
        if (existingDb != null)
        {
            existingDb.AreaId = database.AreaId;
            existingDb.SceneName = database.SceneName;
            existingDb.ChunkWorldSize = database.ChunkWorldSize;
            existingDb.TexturePPU = database.TexturePPU;
            existingDb.ChunkOrigin = database.ChunkOrigin;
            existingDb.SourceTextureWidth = database.SourceTextureWidth;
            existingDb.SourceTextureHeight = database.SourceTextureHeight;
            if (exportGridRootPrefab && gridRootExported)
            {
                existingDb.WalkGridKey = database.WalkGridKey;
            }

            if (!string.IsNullOrEmpty(database.LogicHeightConfigKey))
            {
                existingDb.LogicHeightConfigKey = database.LogicHeightConfigKey;
            }

            existingDb.Chunks = database.Chunks;
            existingDb.LogicWorldRect = database.LogicWorldRect;
            existingDb.InvalidateLookup();
            EditorUtility.SetDirty(existingDb);
            database = existingDb;
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
            Message = BuildSuccessMessage(dbPath, tmCount, gridRootExported, exportTilemap, exportGridRootPrefab, bakeVisualLayers),
            Database = database,
            BackgroundChunkCount = 0,
            TilemapChunkCount = tmCount,
            GridRootPrefabExported = gridRootExported
        };
    }

    static int ComputePaintAtlasWidth(MapChunkEditorRoot editorRoot, int slicePx)
    {
        if (editorRoot == null || slicePx <= 0 || editorRoot.PaintWorldRect.width <= 0f)
        {
            return 0;
        }

        int cols = Mathf.Max(1, Mathf.CeilToInt(editorRoot.PaintWorldRect.width / editorRoot.ChunkWorldSize));
        return cols * slicePx;
    }

    static int ComputePaintAtlasHeight(MapChunkEditorRoot editorRoot, int slicePx)
    {
        if (editorRoot == null || slicePx <= 0 || editorRoot.PaintWorldRect.height <= 0f)
        {
            return 0;
        }

        int rows = Mathf.Max(1, Mathf.CeilToInt(editorRoot.PaintWorldRect.height / editorRoot.ChunkWorldSize));
        return rows * slicePx;
    }

    static string BuildSuccessMessage(
        string dbPath,
        int tmCount,
        bool gridRootExported,
        bool exportTilemap,
        bool exportGridRootPrefab,
        bool bakeVisualLayers)
    {
        var parts = new List<string> { $"database -> {dbPath}" };
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

        parts.Add("background: use Map Paint Background -> Sync");
        return string.Join(", ", parts);
    }

    static HashSet<ChunkCoord> CollectAllChunkCoords(
        Tilemap[] tileGrounds,
        float chunkSize,
        Vector2 origin,
        bool includeTilemap,
        List<MapChunkVisualBaker.BakedLayer> bakedVisualLayers,
        MapChunkEditorRoot editorRoot = null)
    {
        var coords = new HashSet<ChunkCoord>();
        bool hasPlayRect = editorRoot != null &&
                           editorRoot.PaintWorldRect.width > 0f &&
                           editorRoot.PaintWorldRect.height > 0f;
        Rect playRect = hasPlayRect ? editorRoot.PaintWorldRect : default;

        if (hasPlayRect)
        {
            MapChunkUtility.CollectChunkCoordsForWorldRect(playRect, origin, chunkSize, coords);
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
                    var coord = MapChunkUtility.WorldToChunk(world, origin, chunkSize);
                    if (hasPlayRect &&
                        !MapChunkUtility.IsChunkInsideWorldRect(coord, playRect, origin, chunkSize))
                    {
                        continue;
                    }

                    coords.Add(coord);
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
                    var coord = MapChunkUtility.WorldToChunk(world, origin, chunkSize);
                    if (hasPlayRect &&
                        !MapChunkUtility.IsChunkInsideWorldRect(coord, playRect, origin, chunkSize))
                    {
                        continue;
                    }

                    coords.Add(coord);
                }
            }
        }

        if (hasPlayRect)
        {
            coords.RemoveWhere(c => !MapChunkUtility.IsChunkInsideWorldRect(c, playRect, origin, chunkSize));
        }

        return coords;
    }

    static void AssignLogicHeightConfigKey(
        MapChunkDatabase database,
        string mapName,
        string rootFolder,
        MapChunkDatabase existingDb)
    {
        const string defaultAssetPath = "Assets/Resources/MapLogicHeightConfig.asset";
        string destPath = $"{rootFolder}/LogicHeightConfig.asset";
        if (File.Exists(defaultAssetPath))
        {
            if (!File.Exists(destPath))
            {
                AssetDatabase.CopyAsset(defaultAssetPath, destPath);
            }

            database.LogicHeightConfigKey = $"MapChunk/{mapName}/LogicHeightConfig";
            return;
        }

        if (existingDb != null && !string.IsNullOrEmpty(existingDb.LogicHeightConfigKey))
        {
            database.LogicHeightConfigKey = existingDb.LogicHeightConfigKey;
            return;
        }

        database.LogicHeightConfigKey = WorldAreaRoot.DefaultLogicHeightConfigKey;
    }

    static bool ExportGridRootPrefab(MapChunkEditorRoot editorRoot, string rootFolder)
    {
        var gridRoot = MapChunkEditorTilemapResolver.TryGetGridRoot(editorRoot);
        if (gridRoot == null)
        {
            return false;
        }

        var clone = Object.Instantiate(gridRoot.gameObject);
        clone.name = gridRoot.gameObject.name;
        clone.transform.SetPositionAndRotation(gridRoot.position, gridRoot.rotation);
        clone.transform.localScale = gridRoot.localScale;

        bool export3dCollision = MapChunkEditorSettings.GetOrCreate().ExportGridRoot3DCollision;
        if (export3dCollision)
        {
            var editorSettings = MapChunkEditorSettings.GetOrCreate();
            float thickness = Mathf.Max(0.01f, editorSettings.GridRootCollisionThickness);
            string layerName = editorSettings.GridRootCollisionLayer;
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

    static string ExportTilemapChunk(
        ChunkCoord coord,
        float chunkSize,
        Vector2 origin,
        Tilemap[] tileGrounds,
        List<MapChunkVisualBaker.BakedLayer> bakedVisualLayers,
        string rootFolder,
        bool includeLogicLayers)
    {
        var chunkMin = MapChunkUtility.ChunkWorldMin(coord, origin, chunkSize);
        var chunkMax = chunkMin + new Vector3(chunkSize, chunkSize, 0f);

        var go = new GameObject($"tm_{coord.X}_{coord.Y}");
        bool hasTile = false;

        if (includeLogicLayers && tileGrounds != null)
        {
            foreach (var source in tileGrounds)
            {
                if (source == null)
                {
                    continue;
                }

                TryExportLogicLayer(source, go.transform, chunkMin, chunkMax, ref hasTile);
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
        CopyTilemapRenderer(source.GetComponent<TilemapRenderer>(), renderer);

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
        CopyTilemapRenderer(source.GetComponent<TilemapRenderer>(), renderer, baked.SortingOrder);

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

    static void CopyTilemapRenderer(TilemapRenderer source, TilemapRenderer dst, int? sortingOrder = null)
    {
        if (source == null)
        {
            if (sortingOrder.HasValue)
            {
                dst.sortingOrder = sortingOrder.Value;
            }

            return;
        }

        dst.sortingOrder = sortingOrder ?? source.sortingOrder;
        dst.sortingLayerID = source.sortingLayerID;
        dst.mode = source.mode;
    }
}
