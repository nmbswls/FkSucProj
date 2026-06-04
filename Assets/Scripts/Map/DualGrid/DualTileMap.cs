using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class DualTileMap : MonoBehaviour
    {
        const string DefaultViewTilePath = "Assets/Arts/DualTile/DualGridViewTile.asset";

        [HideInInspector] public Grid Grid;
        public Tilemap DataTilemap;
        public DualGridBrushRegistry BrushRegistry;
        public Tilemap ViewTilemap;
        [HideInInspector] public DualGridViewTile ViewTile;
        public bool AutoRefreshInEditor = true;
        [HideInInspector] public int ViewSortingOrder = 10;

        static readonly Vector3Int[] CellBuffer = new Vector3Int[4];
        Tilemap _subscribedData;
        bool _refreshing;

        void Reset()
        {
            DataTilemap ??= transform.Find("Data")?.GetComponent<Tilemap>();
            ViewTilemap ??= transform.Find("View")?.GetComponent<Tilemap>();
            EnsureViewTileAsset();
        }

        public Grid ResolveGrid()
        {
            if (Grid != null)
            {
                return Grid;
            }

            var parent = transform.parent;
            if (parent != null)
            {
                var parentGrid = parent.GetComponentInParent<Grid>();
                if (parentGrid != null)
                {
                    return parentGrid;
                }
            }

            return GetComponent<Grid>();
        }

        void OnEnable()
        {
            EnsureViewTileAsset();
            ApplyRendererSettings();
            SubscribeDataChanges();
            EnsureViewOffset();
            SyncViewTilemapSettings();
            if (AutoRefreshInEditor || Application.isPlaying)
            {
                RefreshAll();
            }
        }

        void OnDisable() => UnsubscribeDataChanges();

        void OnValidate()
        {
            ApplyRendererSettings();
            EnsureViewOffset();
            SyncViewTilemapSettings();
        }

        void EnsureViewTileAsset()
        {
            if (ViewTile != null)
            {
                return;
            }

#if UNITY_EDITOR
            ViewTile = UnityEditor.AssetDatabase.LoadAssetAtPath<DualGridViewTile>(DefaultViewTilePath);
#endif
        }

        public void ApplyRendererSettings()
        {
            if (ViewTilemap == null)
            {
                return;
            }

            var viewRenderer = ViewTilemap.GetComponent<TilemapRenderer>();
            if (viewRenderer == null)
            {
                return;
            }

            viewRenderer.enabled = true;
            if (DataTilemap != null)
            {
                var dataRenderer = DataTilemap.GetComponent<TilemapRenderer>();
                if (dataRenderer != null)
                {
                    viewRenderer.sortingLayerID = dataRenderer.sortingLayerID;
                    viewRenderer.sortingOrder = dataRenderer.sortingOrder + ViewSortingOrder;
                }
            }
            else
            {
                viewRenderer.sortingOrder += ViewSortingOrder;
            }
        }

        void SyncViewTilemapSettings()
        {
            if (DataTilemap == null || ViewTilemap == null)
            {
                return;
            }

            ViewTilemap.tileAnchor = DataTilemap.tileAnchor;
            ViewTilemap.orientation = DataTilemap.orientation;
        }

        public bool IsConfigured(out string error)
        {
            if (DataTilemap == null)
            {
                error = "Data Tilemap 未指定";
                return false;
            }

            if (BrushRegistry == null)
            {
                error = "Brush Registry 未指定";
                return false;
            }

            if (ViewTilemap == null)
            {
                error = "View Tilemap 未指定";
                return false;
            }

            if (ViewTile == null)
            {
                error = "View Tile 未指定（Assets/Arts/DualTile/DualGridViewTile.asset）";
                return false;
            }

            if (BrushRegistry.Terrains == null || BrushRegistry.Terrains.Length == 0)
            {
                error = "Brush Registry / Terrains 为空";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryResolveViewCorner(Vector3Int viewCell, out byte terrainId, out int mask)
        {
            terrainId = 0;
            mask = 0;
            if (DataTilemap == null || BrushRegistry == null)
            {
                return false;
            }

            return BrushRegistry.TryResolveViewCorner(DataTilemap, viewCell, out terrainId, out mask);
        }

        public bool TryResolveAtDataCell(Vector3Int dataCell, out byte terrainId, out int mask)
        {
            return TryResolveViewCorner(dataCell, out terrainId, out mask);
        }

        public Vector3Int WorldToViewCell(Vector3 world)
        {
            return ViewTilemap != null ? ViewTilemap.WorldToCell(world) : Vector3Int.zero;
        }

        public Vector3Int WorldToDataCell(Vector3 world)
        {
            return DataTilemap != null ? DataTilemap.WorldToCell(world) : Vector3Int.zero;
        }

        public bool TryGetViewSprite(Vector3Int viewCell, out Sprite sprite)
        {
            sprite = null;
            if (DataTilemap == null || BrushRegistry == null)
            {
                return false;
            }

            if (!TryResolveViewCorner(viewCell, out byte terrainId, out int mask))
            {
                return false;
            }

            var palette = BrushRegistry.FindPalette(terrainId);
            if (palette == null)
            {
                return false;
            }

            sprite = palette.GetSprite(mask, DualGridCore.StableHash(viewCell));
            return sprite != null;
        }

        public void EnsureViewOffset()
        {
            var grid = ResolveGrid();
            if (grid == null || ViewTilemap == null)
            {
                return;
            }

            var offset = DualGridCore.GetViewLocalOffset(grid.cellSize);
            var t = ViewTilemap.transform;
            if (t.parent == transform || t.IsChildOf(transform))
            {
                t.localPosition = offset;
            }
        }

        void SubscribeDataChanges()
        {
            if (DataTilemap == _subscribedData)
            {
                return;
            }

            UnsubscribeDataChanges();
            if (DataTilemap != null)
            {
                _subscribedData = DataTilemap;
                Tilemap.tilemapTileChanged += OnDataTileChanged;
            }
        }

        void UnsubscribeDataChanges()
        {
            if (_subscribedData == null)
            {
                return;
            }

            Tilemap.tilemapTileChanged -= OnDataTileChanged;
            _subscribedData = null;
        }

        void OnDataTileChanged(Tilemap tilemap, Tilemap.SyncTile[] changes)
        {
            if (_refreshing || !AutoRefreshInEditor && !Application.isPlaying)
            {
                return;
            }

            if (tilemap != DataTilemap || changes == null || ViewTilemap == null || ViewTile == null)
            {
                return;
            }

            for (int i = 0; i < changes.Length; i++)
            {
                RefreshAroundDataCell(changes[i].position);
            }

            ViewTilemap.RefreshAllTiles();
        }

        public void RefreshAroundDataCell(Vector3Int dataCell)
        {
            DualGridCore.GetViewCornersAroundDataCell(dataCell, CellBuffer);
            for (int i = 0; i < 4; i++)
            {
                RefreshViewCell(CellBuffer[i]);
            }
        }

        public void RefreshAroundLogicCell(Vector3Int logicCell) => RefreshAroundDataCell(logicCell);

        public void RefreshViewCell(Vector3Int viewCell)
        {
            if (ViewTilemap == null || ViewTile == null || BrushRegistry == null)
            {
                return;
            }

            if (TryResolveViewCorner(viewCell, out _, out _))
            {
                ViewTilemap.SetTile(viewCell, ViewTile);
            }
            else
            {
                ViewTilemap.SetTile(viewCell, null);
            }

            ViewTilemap.RefreshTile(viewCell);
        }

        public void RefreshAll()
        {
            if (_refreshing || DataTilemap == null || ViewTilemap == null || BrushRegistry == null || ViewTile == null)
            {
                return;
            }

            _refreshing = true;
            try
            {
                RefreshAllInternal();
            }
            finally
            {
                _refreshing = false;
            }
        }

        void RefreshAllInternal()
        {
            ApplyRendererSettings();
            DataTilemap.CompressBounds();

            var bounds = DataTilemap.cellBounds;
            var touched = new HashSet<Vector3Int>();

            if (bounds.size.x > 0 && bounds.size.y > 0)
            {
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (DataTilemap.GetTile(pos) == null)
                    {
                        continue;
                    }

                    DualGridCore.GetViewCornersAroundDataCell(pos, CellBuffer);
                    for (int i = 0; i < 4; i++)
                    {
                        touched.Add(CellBuffer[i]);
                    }
                }
            }

            var viewBounds = ViewTilemap.cellBounds;
            if (viewBounds.size.x > 0 && viewBounds.size.y > 0)
            {
                foreach (var pos in viewBounds.allPositionsWithin)
                {
                    if (!touched.Contains(pos) && ViewTilemap.GetTile(pos) != null)
                    {
                        ViewTilemap.SetTile(pos, null);
                    }
                }
            }

            foreach (var viewCell in touched)
            {
                RefreshViewCell(viewCell);
            }

            ViewTilemap.RefreshAllTiles();
            ViewTilemap.CompressBounds();
        }

        public int CountDataTiles()
        {
            if (DataTilemap == null)
            {
                return 0;
            }

            DataTilemap.CompressBounds();
            var b = DataTilemap.cellBounds;
            if (b.size.x <= 0 || b.size.y <= 0)
            {
                return 0;
            }

            int count = 0;
            foreach (var pos in b.allPositionsWithin)
            {
                if (DataTilemap.GetTile(pos) != null)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountViewTiles()
        {
            if (ViewTilemap == null)
            {
                return 0;
            }

            ViewTilemap.CompressBounds();
            var b = ViewTilemap.cellBounds;
            if (b.size.x <= 0 || b.size.y <= 0)
            {
                return 0;
            }

            int count = 0;
            foreach (var pos in b.allPositionsWithin)
            {
                if (ViewTilemap.GetTile(pos) != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
