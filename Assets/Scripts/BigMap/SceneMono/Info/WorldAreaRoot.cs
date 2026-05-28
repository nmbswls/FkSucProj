using System.Linq;
using My.MapExport;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldAreaRoot : MonoBehaviour
{
    public Grid Grid;

    public Tilemap[] TileGrounds;
    public Tilemap TileHole;

    public Transform PlayerBornPos;

    public Transform StaticPrefabRoot;

    public Transform BackgroundChunkRoot;
    public Transform TilemapChunkRoot;

    GameObject _walkGridInstance;

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
        var tilemaps = _walkGridInstance.GetComponentsInChildren<Tilemap>(true);
        TileHole = tilemaps.FirstOrDefault(t => t.name == "Hole");
        TileGrounds = tilemaps.Where(t => t != TileHole).ToArray();
        DisableTilemapRenderers(tilemaps);

        Debug.Log($"[WorldAreaRoot] Walk grid bound: {resourceKey}, layers={TileGrounds.Length}");
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

        root.BindWalkGrid(chunkDb.WalkGridKey);
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
}
