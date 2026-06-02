using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 从带 TilemapCollider2D 的 Tilemap 烘焙合并后的 3D BoxCollider（XY 平面 + Z 厚度）
public static class TilemapCollision3DGenerator
{
    public const string HolderName = "_XY3D_Collision";

    public struct GenerateResult
    {
        public int TilemapLayerCount;
        public int BoxColliderCount;
    }

    public static GenerateResult GenerateUnderGridRoot(Transform gridRoot, float thickness, int physicsLayer)
    {
        var result = new GenerateResult();
        if (gridRoot == null || thickness <= 0f)
        {
            return result;
        }

        var tilemaps = gridRoot.GetComponentsInChildren<Tilemap>(true);
        foreach (var tilemap in tilemaps)
        {
            if (tilemap == null || !HasTilemapCollisionComponent(tilemap))
            {
                continue;
            }

            int boxCount = GenerateForTilemap(tilemap, thickness, physicsLayer);
            if (boxCount <= 0)
            {
                continue;
            }

            result.TilemapLayerCount++;
            result.BoxColliderCount += boxCount;
        }

        return result;
    }

    static bool HasTilemapCollisionComponent(Tilemap tilemap)
    {
        var col = tilemap.GetComponent<TilemapCollider2D>();
        return col != null && col.enabled;
    }

    static bool CellHasCollision(Tilemap tilemap, Vector3Int cell)
    {
        if (!tilemap.HasTile(cell))
        {
            return false;
        }

        return tilemap.GetColliderType(cell) != Tile.ColliderType.None;
    }

    static int GenerateForTilemap(Tilemap tilemap, float thickness, int physicsLayer)
    {
        RemoveExistingHolder(tilemap.transform);

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            return 0;
        }

        var cells = new HashSet<Vector3Int>();
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (CellHasCollision(tilemap, pos))
            {
                cells.Add(pos);
            }
        }

        if (cells.Count == 0)
        {
            return 0;
        }

        var merged = MergeCellsGreedy(cells);
        if (merged.Count == 0)
        {
            return 0;
        }

        var holder = new GameObject(HolderName);
        holder.transform.SetParent(tilemap.transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;
        holder.layer = physicsLayer >= 0 ? physicsLayer : tilemap.gameObject.layer;

        int index = 0;
        foreach (var rect in merged)
        {
            CreateMergedBox(tilemap, holder.transform, rect, thickness, physicsLayer, index++);
        }

        return merged.Count;
    }

    static void RemoveExistingHolder(Transform tilemapTransform)
    {
        var existing = tilemapTransform.Find(HolderName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
    }

    // 贪心矩形合并：先尽量向右扩，再向下扩成最大矩形
    static List<RectInt> MergeCellsGreedy(HashSet<Vector3Int> cells)
    {
        var used = new HashSet<Vector3Int>();
        var sorted = new List<Vector3Int>(cells);
        sorted.Sort((a, b) =>
        {
            int cy = a.y.CompareTo(b.y);
            return cy != 0 ? cy : a.x.CompareTo(b.x);
        });

        var merged = new List<RectInt>();
        foreach (var start in sorted)
        {
            if (used.Contains(start))
            {
                continue;
            }

            int width = 1;
            while (cells.Contains(new Vector3Int(start.x + width, start.y, start.z)) &&
                   !used.Contains(new Vector3Int(start.x + width, start.y, start.z)))
            {
                width++;
            }

            int height = 1;
            var canGrow = true;
            while (canGrow)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    var probe = new Vector3Int(start.x + dx, start.y + height, start.z);
                    if (!cells.Contains(probe) || used.Contains(probe))
                    {
                        canGrow = false;
                        break;
                    }
                }

                if (canGrow)
                {
                    height++;
                }
            }

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    used.Add(new Vector3Int(start.x + dx, start.y + dy, start.z));
                }
            }

            merged.Add(new RectInt(start.x, start.y, width, height));
        }

        return merged;
    }

    static void CreateMergedBox(
        Tilemap tilemap,
        Transform holder,
        RectInt cellRect,
        float thickness,
        int physicsLayer,
        int index)
    {
        var grid = tilemap.layoutGrid;
        var cellSize = grid != null ? grid.cellSize : Vector3.one;
        var scale = tilemap.transform.lossyScale;
        var scaledCell = new Vector3(
            Mathf.Abs(cellSize.x * scale.x),
            Mathf.Abs(cellSize.y * scale.y),
            1f);

        var minCell = new Vector3Int(cellRect.xMin, cellRect.yMin, 0);
        var maxCell = new Vector3Int(cellRect.xMax - 1, cellRect.yMax - 1, 0);

        var worldMin = tilemap.GetCellCenterWorld(minCell) - scaledCell * 0.5f;
        var worldMax = tilemap.GetCellCenterWorld(maxCell) + scaledCell * 0.5f;

        var worldCenter = new Vector3(
            (worldMin.x + worldMax.x) * 0.5f,
            (worldMin.y + worldMax.y) * 0.5f,
            tilemap.transform.position.z - thickness * 0.5f);

        var worldSize = new Vector3(
            worldMax.x - worldMin.x,
            worldMax.y - worldMin.y,
            thickness);

        var go = new GameObject($"Box_{index}");
        go.transform.SetParent(holder, false);
        go.transform.position = worldCenter;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = physicsLayer >= 0 ? physicsLayer : tilemap.gameObject.layer;
        go.isStatic = true;

        var box = go.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = worldSize;
    }
}
