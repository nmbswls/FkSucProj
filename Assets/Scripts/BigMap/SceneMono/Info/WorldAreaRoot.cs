using System.Collections.Generic;
using My.Map.Ground;
using My.MapExport;
using UnityEngine;
using UnityEngine.Tilemaps;

// 运行时区域上下文：场景内固定 GridRoot / NavMesh，逻辑层与 chunk 内容运行时装配
public class WorldAreaRoot : MonoBehaviour
{
    public const string DefaultLogicHeightConfigKey = "MapLogicHeightConfig";
    public const string SceneGridRootName = "GridRoot";

    Grid _grid;
    Tilemap[] _tileGrounds;
    Tilemap _tileHole;
    MapLogicHeightConfig _logicHeightConfig;

    Transform _staticPrefabRoot;
    Transform _backgroundChunkRoot;
    Transform _tilemapChunkRoot;

    GameObject _walkGridInstance;
    readonly List<GameObject> _walkLayerInstances = new List<GameObject>();

    public Grid Grid => _grid;
    public Tilemap[] TileGrounds => _tileGrounds;
    public Tilemap TileHole => _tileHole;
    public MapLogicHeightConfig LogicHeightConfig => _logicHeightConfig;
    public Transform StaticPrefabRoot => _staticPrefabRoot;
    public Transform BackgroundChunkRoot => _backgroundChunkRoot;
    public Transform TilemapChunkRoot => _tilemapChunkRoot;

    public bool HasWalkTileGrounds => _tileGrounds != null && _tileGrounds.Length > 0;
    public bool HasSceneGridRoot => ResolveSceneGridRoot() != null;

    void Awake()
    {
        EnsureRuntimeHandles();
        EnsureSceneGrid();
    }

    public void Initialize(MapChunkDatabase chunkDb)
    {
        EnsureRuntimeHandles();
        EnsureSceneGrid();

        if (chunkDb != null)
        {
            BindLogicHeightConfig(chunkDb);
        }

        if (_logicHeightConfig == null)
        {
            _logicHeightConfig = Resources.Load<MapLogicHeightConfig>(DefaultLogicHeightConfigKey);
        }

        if (HasSceneGridRoot)
        {
            if (chunkDb != null && chunkDb.HasWalkGrid && !HasWalkTileGrounds)
            {
                PopulateWalkLayersFromPrefab(chunkDb.WalkGridKey);
            }

            ApplyTileGroundsFromLogicHeightConfig();
            return;
        }

        if (chunkDb != null && chunkDb.HasWalkGrid)
        {
            BindWalkGridLegacy(chunkDb.WalkGridKey);
            return;
        }

        if (!HasWalkTileGrounds)
        {
            ApplyTileGroundsFromLogicHeightConfig();
        }
    }

    public void BindWalkGrid(string resourceKey)
    {
        if (HasSceneGridRoot)
        {
            PopulateWalkLayersFromPrefab(resourceKey);
            return;
        }

        BindWalkGridLegacy(resourceKey);
    }

    void PopulateWalkLayersFromPrefab(string resourceKey)
    {
        ClearWalkLayers();
        if (string.IsNullOrEmpty(resourceKey) || _grid == null)
        {
            return;
        }

        var prefab = Resources.Load<GameObject>(resourceKey);
        if (prefab == null)
        {
            Debug.LogError($"[WorldAreaRoot] Walk grid prefab not found: Resources/{resourceKey}");
            return;
        }

        var sourceGrid = prefab.GetComponent<Grid>();
        if (sourceGrid == null)
        {
            Debug.LogError($"[WorldAreaRoot] Walk grid prefab has no Grid: Resources/{resourceKey}");
            return;
        }

        for (int i = 0; i < sourceGrid.transform.childCount; i++)
        {
            var child = sourceGrid.transform.GetChild(i);
            var clone = Instantiate(child.gameObject, _grid.transform);
            clone.name = child.name;
            clone.transform.localPosition = child.localPosition;
            clone.transform.localRotation = child.localRotation;
            clone.transform.localScale = child.localScale;
            _walkLayerInstances.Add(clone);
        }

        ResolveTileHoleFromGrid();
        ApplyTileGroundsFromLogicHeightConfig();

        Debug.Log(
            $"[WorldAreaRoot] Walk layers populated under scene GridRoot: {resourceKey}, " +
            $"groundLayers={_tileGrounds?.Length ?? 0}");
    }

    void BindWalkGridLegacy(string resourceKey)
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

        _grid = _walkGridInstance.GetComponent<Grid>();
        ResolveTileHoleFromGrid();
        ApplyTileGroundsFromLogicHeightConfig();

        Debug.Log(
            $"[WorldAreaRoot] Walk grid bound (legacy): {resourceKey}, groundLayers={_tileGrounds?.Length ?? 0}");
    }

    public void ClearWalkGrid()
    {
        ClearWalkLayers();

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

        if (!HasSceneGridRoot)
        {
            _grid = null;
            _tileGrounds = null;
            _tileHole = null;
        }
    }

    void ClearWalkLayers()
    {
        for (int i = _walkLayerInstances.Count - 1; i >= 0; i--)
        {
            var go = _walkLayerInstances[i];
            if (go == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        _walkLayerInstances.Clear();
        _tileGrounds = null;
        _tileHole = null;
    }

    public bool ApplyTileGroundsFromLogicHeightConfig()
    {
        if (_logicHeightConfig == null)
        {
            Debug.LogWarning("[WorldAreaRoot] LogicHeightConfig is missing, cannot assemble ground layers.");
            return false;
        }

        if (_logicHeightConfig.GroundLayerNames == null || _logicHeightConfig.GroundLayerNames.Length == 0)
        {
            Debug.LogWarning("[WorldAreaRoot] LogicHeightConfig.GroundLayerNames is empty.");
            return false;
        }

        EnsureSceneGrid();
        if (_grid == null)
        {
            Debug.LogWarning("[WorldAreaRoot] Grid not found, cannot assemble ground layers.");
            return false;
        }

        ResolveTileHoleFromGrid();
        _tileGrounds = CollectGroundTilemaps(_grid, _tileHole, _logicHeightConfig);
        if (_tileGrounds == null || _tileGrounds.Length == 0)
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
        return IsWorldPosWalkable(worldPos, _tileGrounds, _tileHole);
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
        if (_tileGrounds != null && _tileGrounds.Length > 0)
        {
            return _tileGrounds;
        }

        if (_logicHeightConfig != null)
        {
            ApplyTileGroundsFromLogicHeightConfig();
        }

        return _tileGrounds;
    }

    void BindLogicHeightConfig(MapChunkDatabase chunkDb)
    {
        var key = !string.IsNullOrEmpty(chunkDb.LogicHeightConfigKey)
            ? chunkDb.LogicHeightConfigKey
            : DefaultLogicHeightConfigKey;

        _logicHeightConfig = Resources.Load<MapLogicHeightConfig>(key);
        if (_logicHeightConfig == null)
        {
            Debug.LogWarning($"[WorldAreaRoot] LogicHeightConfig not found: Resources/{key}");
        }
    }

    void EnsureRuntimeHandles()
    {
        if (_staticPrefabRoot == null)
        {
            var staticRoot = transform.Find("StaticRoot");
            if (staticRoot != null)
            {
                _staticPrefabRoot = staticRoot;
            }
        }

        if (_backgroundChunkRoot == null)
        {
            var existing = transform.Find("BackgroundChunkRoot");
            if (existing != null)
            {
                _backgroundChunkRoot = existing;
            }
            else
            {
                var go = new GameObject("BackgroundChunkRoot");
                go.transform.SetParent(transform, false);
                _backgroundChunkRoot = go.transform;
            }
        }

        if (_tilemapChunkRoot == null)
        {
            var existing = transform.Find("TilemapChunkRoot");
            if (existing != null)
            {
                _tilemapChunkRoot = existing;
            }
            else
            {
                var go = new GameObject("TilemapChunkRoot");
                go.transform.SetParent(transform, false);
                _tilemapChunkRoot = go.transform;
            }
        }
    }

    Transform ResolveSceneGridRoot()
    {
        if (_staticPrefabRoot != null)
        {
            var underStatic = _staticPrefabRoot.Find(SceneGridRootName);
            if (underStatic != null)
            {
                return underStatic;
            }
        }

        return transform.Find(SceneGridRootName);
    }

    void EnsureSceneGrid()
    {
        if (_grid != null)
        {
            return;
        }

        var gridRoot = ResolveSceneGridRoot();
        if (gridRoot != null)
        {
            _grid = gridRoot.GetComponent<Grid>();
        }

        if (_grid == null)
        {
            _grid = GetComponentInChildren<Grid>(true);
        }
    }

    void ResolveTileHoleFromGrid()
    {
        if (_tileHole != null || _grid == null)
        {
            return;
        }

        foreach (var tm in _grid.GetComponentsInChildren<Tilemap>(true))
        {
            if (tm != null && tm.name == "Hole")
            {
                _tileHole = tm;
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

#if UNITY_EDITOR
    public void EditorResolveFromSceneHierarchy()
    {
        EnsureRuntimeHandles();
        EnsureSceneGrid();
        if (_logicHeightConfig == null)
        {
            _logicHeightConfig = Resources.Load<MapLogicHeightConfig>(DefaultLogicHeightConfigKey);
        }

        ApplyTileGroundsFromLogicHeightConfig();
    }

    public void EditorSetTileGrounds(Tilemap[] tileGrounds)
    {
        _tileGrounds = tileGrounds;
    }
#endif
}
