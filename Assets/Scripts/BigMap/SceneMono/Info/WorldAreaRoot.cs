using System.Collections.Generic;
using My.Map.Ground;
using My.MapExport;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldAreaRoot : MonoBehaviour
{
    public Grid Grid;

    // 由 MapLogicHeightConfig.GroundLayerNames 装配，勿在 Inspector 手拖全量 Tilemap。
    public Tilemap[] TileGrounds;
    public Tilemap TileHole;

    [Header("Logic Height")]
    public MapLogicHeightConfig LogicHeightConfig;

    public Transform PlayerBornPos;

    public Transform StaticPrefabRoot;

    public Transform BackgroundChunkRoot;
    public Transform TilemapChunkRoot;

    [Header("Camera Bounds")]
    [Tooltip("留空则使用 MapChunkDatabase.LogicWorldRect（或由 Chunks 推算）")]
    public Rect LogicWorldRectOverride;

    GameObject _walkGridInstance;

    public bool HasLogicWorldRectOverride =>
        LogicWorldRectOverride.width > 0f && LogicWorldRectOverride.height > 0f;

    public bool HasWalkTileGrounds => TileGrounds != null && TileGrounds.Length > 0;

    void Awake()
    {
        if (BackgroundChunkRoot == null)
        {
            var go = new GameObject("BackgroundChunkRoot");
            go.transform.SetParent(transform, false);
            BackgroundChunkRoot = go.transform;
        }

        if (TilemapChunkRoot == null)
        {
            var go = new GameObject("TilemapChunkRoot");
            go.transform.SetParent(transform, false);
            TilemapChunkRoot = go.transform;
        }
    }

    public void BindWalkGrid(string resourceKey)
    {
        ClearWalkGrid();
        if (string.IsNullOrEmpty(resourceKey))
        {
            return;
        }

        var prefab = Resources.Load<GameObject>(resourceKey);
        if (prefab == null)
        {
            Debug.LogError($"[WorldAreaRoot] Walk grid prefab not found: Resources/{resourceKey}");
            return;
        }

        _walkGridInstance = Instantiate(prefab, transform);
        _walkGridInstance.name = "WalkGridRoot";
        _walkGridInstance.transform.localPosition = Vector3.zero;
        _walkGridInstance.transform.localRotation = Quaternion.identity;
        _walkGridInstance.transform.localScale = Vector3.one;

        Grid = _walkGridInstance.GetComponent<Grid>();
        ResolveTileHoleFromGrid();
        ApplyTileGroundsFromLogicHeightConfig();

        var allTilemaps = _walkGridInstance.GetComponentsInChildren<Tilemap>(true);
        DisableTilemapRenderers(allTilemaps);

        Debug.Log(
            $"[WorldAreaRoot] Walk grid bound: {resourceKey}, groundLayers={TileGrounds?.Length ?? 0} " +
            $"(from LogicHeightConfig)");
    }

    public void ClearWalkGrid()
    {
        if (_walkGridInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_walkGridInstance);
            }
            else
            {
                DestroyImmediate(_walkGridInstance);
            }
        }

        _walkGridInstance = null;
    }

    // 按 LogicHeightConfig.GroundLayerNames 从 Grid 下解析地面 Tilemap（仅地面层）。
    public bool ApplyTileGroundsFromLogicHeightConfig()
    {
        if (LogicHeightConfig == null)
        {
            Debug.LogWarning("[WorldAreaRoot] LogicHeightConfig is missing, cannot assemble ground layers.");
            return false;
        }

        if (LogicHeightConfig.GroundLayerNames == null || LogicHeightConfig.GroundLayerNames.Length == 0)
        {
            Debug.LogWarning("[WorldAreaRoot] LogicHeightConfig.GroundLayerNames is empty.");
            return false;
        }

        EnsureGridReference();
        if (Grid == null)
        {
            Debug.LogWarning("[WorldAreaRoot] Grid not found, cannot assemble ground layers.");
            return false;
        }

        ResolveTileHoleFromGrid();
        TileGrounds = CollectGroundTilemaps(Grid, TileHole, LogicHeightConfig);
        if (TileGrounds == null || TileGrounds.Length == 0)
        {
            Debug.LogWarning("[WorldAreaRoot] No ground tilemaps matched GroundLayerNames.");
            return false;
        }

        return true;
    }

    public static Tilemap[] CollectGroundTilemaps(Grid grid, Tilemap tileHole, MapLogicHeightConfig config)
    {
        if (grid == null || config?.GroundLayerNames == null || config.GroundLayerNames.Length == 0)
        {
            return null;
        }

        var result = new List<Tilemap>();
        foreach (var layerName in config.GroundLayerNames)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                continue;
            }

            if (tileHole != null && tileHole.name == layerName)
            {
                continue;
            }

            var found = FindTilemapUnderGrid(grid, layerName);
            if (found == null)
            {
                Debug.LogWarning($"[WorldAreaRoot] Ground layer not found under Grid: {layerName}");
                continue;
            }

            if (found == tileHole)
            {
                continue;
            }

            result.Add(found);
        }

        return result.Count > 0 ? result.ToArray() : null;
    }

    public bool IsWorldPosWalkableOnTileGrounds(Vector3 worldPos)
    {
        return IsWorldPosWalkable(worldPos, TileGrounds, TileHole);
    }

    public static void TryBindWalkGridFromDatabase(WorldAreaRoot root, MapChunkDatabase chunkDb)
    {
        if (root == null || chunkDb == null || !chunkDb.HasWalkGrid)
        {
            return;
        }

        TryBindLogicHeightConfigFromDatabase(root, chunkDb);
        root.BindWalkGrid(chunkDb.WalkGridKey);
    }

    public static void TryBindLogicHeightConfigFromDatabase(WorldAreaRoot root, MapChunkDatabase chunkDb)
    {
        if (root == null || chunkDb == null || string.IsNullOrEmpty(chunkDb.LogicHeightConfigKey))
        {
            return;
        }

        if (root.LogicHeightConfig != null)
        {
            return;
        }

        var config = Resources.Load<MapLogicHeightConfig>(chunkDb.LogicHeightConfigKey);
        if (config != null)
        {
            root.LogicHeightConfig = config;
        }
        else
        {
            Debug.LogWarning($"[WorldAreaRoot] LogicHeightConfig not found: Resources/{chunkDb.LogicHeightConfigKey}");
        }
    }

    static void DisableTilemapRenderers(Tilemap[] tilemaps)
    {
        if (tilemaps == null)
        {
            return;
        }

        foreach (var tilemap in tilemaps)
        {
            if (tilemap == null)
            {
                continue;
            }

            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    public static bool IsWorldPosWalkable(Vector3 worldPos, Tilemap[] tileGrounds, Tilemap tileHole)
    {
        if (tileGrounds == null || tileGrounds.Length == 0)
        {
            return false;
        }

        if (tileHole != null)
        {
            var holeCell = tileHole.WorldToCell(worldPos);
            if (tileHole.cellBounds.Contains(holeCell) && tileHole.GetTile(holeCell) != null)
            {
                return false;
            }
        }

        foreach (var ground in tileGrounds)
        {
            if (ground == null)
            {
                continue;
            }

            var cell = ground.WorldToCell(worldPos);
            if (!ground.cellBounds.Contains(cell))
            {
                continue;
            }

            if (ground.GetTile(cell) != null)
            {
                return true;
            }
        }

        return false;
    }

    public Tilemap[] ResolveGroundSamplingTilemaps()
    {
        if (TileGrounds != null && TileGrounds.Length > 0)
        {
            return TileGrounds;
        }

        if (LogicHeightConfig != null)
        {
            ApplyTileGroundsFromLogicHeightConfig();
        }

        return TileGrounds;
    }

    void EnsureGridReference()
    {
        if (Grid != null)
        {
            return;
        }

        if (_walkGridInstance != null)
        {
            Grid = _walkGridInstance.GetComponent<Grid>();
            if (Grid != null)
            {
                return;
            }
        }

        Grid = GetComponentInChildren<Grid>(true);
        if (Grid != null)
        {
            return;
        }

        if (StaticPrefabRoot == null)
        {
            return;
        }

        var gridRoot = StaticPrefabRoot.Find("GridRoot");
        if (gridRoot != null)
        {
            Grid = gridRoot.GetComponent<Grid>();
        }
    }

    void ResolveTileHoleFromGrid()
    {
        if (TileHole != null || Grid == null)
        {
            return;
        }

        foreach (var tm in Grid.GetComponentsInChildren<Tilemap>(true))
        {
            if (tm != null && tm.name == "Hole")
            {
                TileHole = tm;
                return;
            }
        }
    }

    static Tilemap FindTilemapUnderGrid(Grid grid, string layerName)
    {
        if (grid == null || string.IsNullOrEmpty(layerName))
        {
            return null;
        }

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            if (tm != null && tm.name == layerName)
            {
                return tm;
            }
        }

        return null;
    }
}
