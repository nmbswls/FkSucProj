using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace My.Map.DualGrid
{
    [DisallowMultipleComponent]
    public class DualTileMap : MonoBehaviour
    {
        [Serializable]
        public class ViewLayer
        {
            public byte TerrainId;
            public Tilemap ViewTilemap;
            public DualGridTile DisplayTile;
            public DualGridTilePalette Palette;

            public byte ResolveTerrainId()
            {
                if (TerrainId != 0)
                {
                    return TerrainId;
                }

                if (Palette != null)
                {
                    return Palette.TerrainId;
                }

                return DisplayTile != null && DisplayTile.Palette != null
                    ? DisplayTile.Palette.TerrainId
                    : (byte)0;
            }
        }

        public Grid Grid;
        public Tilemap DataTilemap;
        [FormerlySerializedAs("TerrainRegistry")]
        public DualGridBrushRegistry BrushRegistry;
        public ViewLayer[] ViewLayers = Array.Empty<ViewLayer>();
        public bool AutoRefreshInEditor = true;

        static readonly Vector3Int[] CellBuffer = new Vector3Int[4];
        Tilemap _subscribedData;

        void Reset()
        {
            DataTilemap ??= transform.Find("Data")?.GetComponent<Tilemap>();
        }

        // 优先父级 Grid；Inspector 手动赋值可覆盖
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
            SubscribeDataChanges();
            EnsureViewOffset();
        }

        void OnDisable() => UnsubscribeDataChanges();

        void OnValidate()
        {
            EnsureViewOffset();
#if UNITY_EDITOR
            if (!Application.isPlaying && AutoRefreshInEditor)
            {
                RefreshAll();
            }
#endif
        }

        public void EnsureViewOffset()
        {
            var grid = ResolveGrid();
            if (grid == null || ViewLayers == null)
            {
                return;
            }

            var offset = DualGridCore.GetViewLocalOffset(grid.cellSize);
            foreach (var layer in ViewLayers)
            {
                if (layer?.ViewTilemap == null)
                {
                    continue;
                }

                var t = layer.ViewTilemap.transform;
                if (t.parent == transform || t.IsChildOf(transform))
                {
                    t.localPosition = offset;
                }
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
            if (DataTilemap == null || BrushRegistry == null || ViewLayers == null)
            {
                return;
            }

            foreach (var layer in ViewLayers)
            {
                RefreshViewCell(layer, viewCell);
            }
        }

        void RefreshViewCell(ViewLayer layer, Vector3Int viewCell)
        {
            if (layer?.ViewTilemap == null || layer.DisplayTile == null)
            {
                return;
            }

            byte terrainId = layer.ResolveTerrainId();
            int mask = DualGridCore.ComputeCornerMask(DataTilemap, BrushRegistry, viewCell, terrainId);
            layer.ViewTilemap.SetTile(viewCell, mask != 0 ? layer.DisplayTile : null);
            layer.ViewTilemap.RefreshTile(viewCell);
        }

        public void RefreshAll()
        {
            if (DataTilemap == null)
            {
                return;
            }

            var b = DataTilemap.cellBounds;
            if (b.size.x <= 0 || b.size.y <= 0)
            {
                return;
            }

            for (int x = b.min.x; x < b.max.x; x++)
            {
                for (int y = b.min.y; y < b.max.y; y++)
                {
                    RefreshViewCell(new Vector3Int(x, y, b.min.z));
                }
            }
        }

        public void RefreshBounds(BoundsInt logicBounds)
        {
            var min = logicBounds.min;
            var max = logicBounds.max;
            for (int x = min.x; x < max.x; x++)
            {
                for (int y = min.y; y < max.y; y++)
                {
                    RefreshViewCell(new Vector3Int(x, y, min.z));
                }
            }
        }

        public Vector3Int WorldToLogicCell(Vector3 world)
        {
            return DataTilemap != null ? DataTilemap.WorldToCell(world) : Vector3Int.zero;
        }

        public Vector3Int WorldToViewCell(Vector3 world)
        {
            var view = ViewLayers != null && ViewLayers.Length > 0 ? ViewLayers[0].ViewTilemap : null;
            return view != null ? view.WorldToCell(world) : WorldToLogicCell(world);
        }
    }
}
