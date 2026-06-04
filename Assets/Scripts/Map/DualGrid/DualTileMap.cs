using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [DisallowMultipleComponent]
    public class DualTileMap : MonoBehaviour
    {
        public Grid Grid;
        public Tilemap DataTilemap;
        public DualGridBrushRegistry BrushRegistry;
        public Tilemap ViewTilemap;
        public bool AutoRefreshInEditor = true;
        public bool HideDataRenderer = true;
        public int ViewSortingOrder = 1;

        static readonly Vector3Int[] CellBuffer = new Vector3Int[4];
        Tilemap _subscribedData;
        DualGridViewTile _viewTile;

        void Reset()
        {
            DataTilemap ??= transform.Find("Data")?.GetComponent<Tilemap>();
            ViewTilemap ??= transform.Find("View")?.GetComponent<Tilemap>();
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
            EnsureViewTile();
            ApplyRendererSettings();
            SubscribeDataChanges();
            EnsureViewOffset();
            RefreshAll();
        }

        void OnDisable() => UnsubscribeDataChanges();

        void OnValidate()
        {
            EnsureViewTile();
            ApplyRendererSettings();
            EnsureViewOffset();
#if UNITY_EDITOR
            if (!Application.isPlaying && AutoRefreshInEditor)
            {
                RefreshAll();
            }
#endif
        }

        void EnsureViewTile()
        {
            if (_viewTile == null)
            {
                _viewTile = ScriptableObject.CreateInstance<DualGridViewTile>();
            }

            _viewTile.Owner = this;
        }

        void ApplyRendererSettings()
        {
            if (DataTilemap != null)
            {
                var dataRenderer = DataTilemap.GetComponent<TilemapRenderer>();
                if (dataRenderer != null)
                {
                    dataRenderer.enabled = !HideDataRenderer;
                    dataRenderer.sortingOrder = 0;
                }
            }

            if (ViewTilemap != null)
            {
                var viewRenderer = ViewTilemap.GetComponent<TilemapRenderer>();
                if (viewRenderer != null)
                {
                    viewRenderer.enabled = true;
                    viewRenderer.sortingOrder = ViewSortingOrder;
                }
            }
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

            if (BrushRegistry.Terrains == null || BrushRegistry.Terrains.Length == 0)
            {
                error = "Brush Registry / Terrains 为空";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryGetViewSprite(Vector3Int viewCell, out Sprite sprite)
        {
            sprite = null;
            if (DataTilemap == null || BrushRegistry == null)
            {
                return false;
            }

            if (!BrushRegistry.TryResolveViewCorner(DataTilemap, viewCell, out byte terrainId, out int mask))
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
            if (tilemap != DataTilemap || changes == null)
            {
                return;
            }

            for (int i = 0; i < changes.Length; i++)
            {
                RefreshAroundLogicCell(changes[i].position);
            }

            ViewTilemap.RefreshAllTiles();
        }

        public void RefreshAroundLogicCell(Vector3Int logicCell)
        {
            DualGridCore.GetViewCornersAroundLogicCell(logicCell, CellBuffer);
            for (int i = 0; i < 4; i++)
            {
                RefreshViewCell(CellBuffer[i]);
            }
        }

        public void RefreshViewCell(Vector3Int viewCell)
        {
            if (ViewTilemap == null || BrushRegistry == null)
            {
                return;
            }

            EnsureViewTile();

            if (BrushRegistry.TryResolveViewCorner(DataTilemap, viewCell, out _, out _))
            {
                ViewTilemap.SetTile(viewCell, _viewTile);
            }
            else
            {
                ViewTilemap.SetTile(viewCell, null);
            }

            ViewTilemap.RefreshTile(viewCell);
        }

        public void RefreshAll()
        {
            if (DataTilemap == null || ViewTilemap == null || BrushRegistry == null)
            {
                return;
            }

            EnsureViewTile();
            DataTilemap.CompressBounds();

            var b = DataTilemap.cellBounds;
            if (b.size.x <= 0 || b.size.y <= 0)
            {
                ViewTilemap.ClearAllTiles();
                return;
            }

            for (int x = b.min.x; x < b.max.x; x++)
            {
                for (int y = b.min.y; y < b.max.y; y++)
                {
                    var logicCell = new Vector3Int(x, y, b.min.z);
                    if (DataTilemap.GetTile(logicCell) == null)
                    {
                        continue;
                    }

                    DualGridCore.GetViewCornersAroundLogicCell(logicCell, CellBuffer);
                    for (int i = 0; i < 4; i++)
                    {
                        RefreshViewCell(CellBuffer[i]);
                    }
                }
            }

            ViewTilemap.RefreshAllTiles();
            ViewTilemap.CompressBounds();
        }

        public void RefreshBounds(BoundsInt logicBounds)
        {
            var min = logicBounds.min;
            var max = logicBounds.max;
            for (int x = min.x; x < max.x; x++)
            {
                for (int y = min.y; y < max.y; y++)
                {
                    RefreshAroundLogicCell(new Vector3Int(x, y, min.z));
                }
            }

            ViewTilemap.RefreshAllTiles();
        }

        public Vector3Int WorldToLogicCell(Vector3 world)
        {
            return DataTilemap != null ? DataTilemap.WorldToCell(world) : Vector3Int.zero;
        }

        public Vector3Int WorldToViewCell(Vector3 world)
        {
            return ViewTilemap != null ? ViewTilemap.WorldToCell(world) : WorldToLogicCell(world);
        }

        sealed class DualGridViewTile : TileBase
        {
            public DualTileMap Owner;

            public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
            {
                tileData.sprite = null;
                tileData.transform = Matrix4x4.identity;
                tileData.color = Color.white;
                tileData.flags = TileFlags.LockColor;
                tileData.colliderType = Tile.ColliderType.None;

                if (Owner == null || !Owner.TryGetViewSprite(position, out var sprite))
                {
                    return;
                }

                tileData.sprite = sprite;
            }

            public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go) => true;
        }
    }
}
