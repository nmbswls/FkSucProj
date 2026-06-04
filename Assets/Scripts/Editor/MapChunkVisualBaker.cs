#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 导出前将 DualGrid View / RuleTile 等视觉层 bake 成静态 Tile，供 tm_* chunk 使用
public static class MapChunkVisualBaker
{
    public class BakedLayer
    {
        public string Name;
        public Tilemap SourceTilemap;
        public int SortingOrder;
        public Dictionary<Vector3Int, BakedCell> Cells = new Dictionary<Vector3Int, BakedCell>();
    }

    public struct BakedCell
    {
        public Tile TileAsset;
        public Matrix4x4 Transform;
    }

    public static List<BakedLayer> Bake(MapChunkEditorRoot editorRoot, string bakedTileFolder, Tilemap[] logicLayers)
    {
        var result = new List<BakedLayer>();
        if (editorRoot == null)
        {
            return result;
        }

        var gridRoot = MapChunkEditorTilemapResolver.TryGetGridRoot(editorRoot);
        if (gridRoot == null)
        {
            Debug.LogWarning("[MapChunkVisualBake] GridRoot not found, skip visual bake.");
            return result;
        }

        var logicSet = new HashSet<Tilemap>();
        if (logicLayers != null)
        {
            foreach (var tm in logicLayers)
            {
                if (tm != null)
                {
                    logicSet.Add(tm);
                }
            }
        }

        var tileCache = new Dictionary<Sprite, Tile>();
        EnsureFolder(bakedTileFolder);

        foreach (var dual in gridRoot.GetComponentsInChildren<DualTileMap>(true))
        {
            BakeDualGridView(dual, result, bakedTileFolder, tileCache);
        }

        foreach (var tm in gridRoot.GetComponentsInChildren<Tilemap>(true))
        {
            if (tm == null || logicSet.Contains(tm))
            {
                continue;
            }

            if (IsDualGridInternalLayer(tm))
            {
                continue;
            }

            if (IsDualGridViewLayer(tm))
            {
                continue;
            }

            BakeTilemapVisual(tm, result, bakedTileFolder, tileCache);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MapChunkVisualBake] Baked {result.Count} visual layer(s), {tileCache.Count} unique tile asset(s).");
        return result;
    }

    static void BakeDualGridView(
        DualTileMap dual,
        List<BakedLayer> result,
        string bakedTileFolder,
        Dictionary<Sprite, Tile> tileCache)
    {
        if (dual == null || dual.ViewTilemap == null)
        {
            return;
        }

        if (!dual.IsConfigured(out var error))
        {
            Debug.LogWarning($"[MapChunkVisualBake] DualTileMap skip: {error}");
            return;
        }

        dual.RefreshAll();

        var source = dual.ViewTilemap;
        var layer = CreateLayer(source, $"Baked_{source.name}");
        source.CompressBounds();

        var bounds = source.cellBounds;
        ExpandBounds(ref bounds, 1);

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!dual.TryGetViewSprite(pos, out var sprite) || sprite == null)
            {
                continue;
            }

            var tile = GetOrCreateTileAsset(sprite, bakedTileFolder, tileCache);
            if (tile == null)
            {
                continue;
            }

            layer.Cells[pos] = new BakedCell
            {
                TileAsset = tile,
                Transform = Matrix4x4.identity,
            };
        }

        if (layer.Cells.Count > 0)
        {
            result.Add(layer);
        }
    }

    static void BakeTilemapVisual(
        Tilemap source,
        List<BakedLayer> result,
        string bakedTileFolder,
        Dictionary<Sprite, Tile> tileCache)
    {
        source.CompressBounds();
        var bounds = source.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            return;
        }

        var layer = CreateLayer(source, $"Baked_{source.name}");
        ExpandBounds(ref bounds, 1);

        foreach (var pos in bounds.allPositionsWithin)
        {
            var baseTile = source.GetTile(pos);
            if (baseTile == null)
            {
                continue;
            }

            if (baseTile is DualGridViewTile)
            {
                continue;
            }

            source.RefreshTile(pos);
            var data = new TileData();
            baseTile.GetTileData(pos, source, ref data);
            if (data.sprite == null)
            {
                continue;
            }

            var tile = GetOrCreateTileAsset(data.sprite, bakedTileFolder, tileCache);
            if (tile == null)
            {
                continue;
            }

            layer.Cells[pos] = new BakedCell
            {
                TileAsset = tile,
                Transform = data.transform,
            };
        }

        if (layer.Cells.Count > 0)
        {
            result.Add(layer);
        }
    }

    static BakedLayer CreateLayer(Tilemap source, string layerName)
    {
        var renderer = source.GetComponent<TilemapRenderer>();
        return new BakedLayer
        {
            Name = layerName,
            SourceTilemap = source,
            SortingOrder = renderer != null ? renderer.sortingOrder : 0,
        };
    }

    static bool IsDualGridInternalLayer(Tilemap tm)
    {
        if (tm == null)
        {
            return false;
        }

        if (tm.name == "Data" && tm.GetComponentInParent<DualTileMap>() != null)
        {
            return true;
        }

        return false;
    }

    static bool IsDualGridViewLayer(Tilemap tm)
    {
        if (tm == null)
        {
            return false;
        }

        return tm.name == "View" && tm.GetComponentInParent<DualTileMap>() != null;
    }

    static Tile GetOrCreateTileAsset(Sprite sprite, string folder, Dictionary<Sprite, Tile> cache)
    {
        if (sprite == null)
        {
            return null;
        }

        if (cache.TryGetValue(sprite, out var cached))
        {
            return cached;
        }

        var spritePath = AssetDatabase.GetAssetPath(sprite);
        var guid = string.IsNullOrEmpty(spritePath) ? sprite.GetInstanceID().ToString() : AssetDatabase.AssetPathToGUID(spritePath);
        string safeName = MakeSafeFileName(sprite.name);
        string assetPath = $"{folder}/baked_{guid}_{safeName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (existing != null)
        {
            cache[sprite] = existing;
            return existing;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, assetPath);
        cache[sprite] = tile;
        return tile;
    }

    static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "sprite";
        }

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    static void ExpandBounds(ref BoundsInt bounds, int margin)
    {
        if (margin <= 0)
        {
            return;
        }

        bounds.xMin -= margin;
        bounds.yMin -= margin;
        bounds.xMax += margin;
        bounds.yMax += margin;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
