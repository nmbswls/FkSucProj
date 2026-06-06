using System.Collections.Generic;
using My.Map.DualGrid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff
{
    public enum CliffPlateauMode
    {
        Standard = 0,
        DualGrid = 1,
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CliffPlateauGenerator : MonoBehaviour
    {
        public const string DefaultCliffChildName = "Cliff";

        public CliffTileSet TileSet;
        [Min(1)] public int Height = 2;

        [Header("Dual Grid")]
        public bool AutoDualGridOffset = true;
        public bool UseDualGridOffset;
        public bool ClearBeforeGenerate = true;

        [Header("Gizmo")]
        public bool DrawCliffGizmo = true;

        public CliffPlateauMode PlateauMode
        {
            get
            {
                var dual = ResolveDual();
                return dual != null && dual.DataTilemap != null
                    ? CliffPlateauMode.DualGrid
                    : CliffPlateauMode.Standard;
            }
        }

        public bool IsDualGridPlateau => PlateauMode == CliffPlateauMode.DualGrid;

        public DualTileMap ResolveDual() =>
            GetComponent<DualTileMap>() ?? GetComponentInParent<DualTileMap>();

        // Dual：View 南缘检测 + Cliff 落砖；Standard：自身 Tilemap
        public Tilemap SourceTilemap => ResolveSourceTilemap();

        public Tilemap CliffTilemap => ResolveCliffTilemap();

        public Tilemap ResolveSourceTilemap()
        {
            var dual = ResolveDual();
            if (dual != null && dual.ViewTilemap != null)
            {
                return dual.ViewTilemap;
            }

            return GetComponent<Tilemap>();
        }

        public void EnsureStandardComponents()
        {
            if (IsDualGridPlateau)
            {
                return;
            }

            EnsureTilemapComponents(gameObject);
        }

        public void EnsureCliffChild()
        {
            var cliffTransform = transform.Find(DefaultCliffChildName);
            if (cliffTransform == null)
            {
                var go = new GameObject(DefaultCliffChildName);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Cliff Tilemap");
                }
#endif
                go.transform.SetParent(transform, false);
                cliffTransform = go.transform;
            }

            var cliffGo = cliffTransform.gameObject;
            EnsureTilemapComponents(cliffGo);
            ApplyCliffRendererDefaults(cliffGo.GetComponent<TilemapRenderer>());
        }

        static void EnsureTilemapComponents(GameObject go)
        {
            if (go.GetComponent<Tilemap>() != null && go.GetComponent<TilemapRenderer>() != null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (go.GetComponent<Tilemap>() == null)
                {
                    UnityEditor.Undo.AddComponent<Tilemap>(go);
                }

                if (go.GetComponent<TilemapRenderer>() == null)
                {
                    UnityEditor.Undo.AddComponent<TilemapRenderer>(go);
                }

                return;
            }
#endif
            if (go.GetComponent<Tilemap>() == null)
            {
                go.AddComponent<Tilemap>();
            }

            if (go.GetComponent<TilemapRenderer>() == null)
            {
                go.AddComponent<TilemapRenderer>();
            }
        }

        public void SyncDualGridOffset()
        {
            EnsureCliffChild();

            var cliff = CliffTilemap;
            var source = SourceTilemap;
            if (cliff == null)
            {
                return;
            }

            bool shouldOffset = UseDualGridOffset;
            if (AutoDualGridOffset && IsDualGridPlateau)
            {
                shouldOffset = true;
                UseDualGridOffset = true;
            }

            var grid = cliff.layoutGrid ?? source?.layoutGrid ?? ResolveDual()?.ResolveGrid();
            var cellSize = grid != null ? grid.cellSize : Vector3.one;

            if (source != null)
            {
                CliffDualGridMapping.SyncCliffTilemapSettings(source, cliff);
            }

            cliff.transform.localPosition = CliffDualGridMapping.GetCliffLocalOffset(cellSize, shouldOffset);
        }

        public void ClearCliffs()
        {
            EnsureCliffChild();

            var cliff = CliffTilemap;
            if (cliff == null)
            {
                return;
            }

            cliff.ClearAllTiles();
        }

        public int GenerateCliffs()
        {
            if (IsDualGridPlateau)
            {
                EnsureCliffChild();
            }
            else
            {
                EnsureStandardComponents();
                EnsureCliffChild();
            }

            var source = SourceTilemap;
            var cliff = CliffTilemap;
            if (source == null || cliff == null || TileSet == null)
            {
                Debug.LogWarning("[CliffPlateauGenerator] Source, Cliff child or TileSet is missing.");
                return 0;
            }

            if (Height < 1)
            {
                Debug.LogWarning("[CliffPlateauGenerator] Height must be >= 1.");
                return 0;
            }

            SyncDualGridOffset();
            if (IsDualGridPlateau && !UseDualGridOffset)
            {
                Debug.LogWarning("[CliffPlateauGenerator] Dual Grid plateau requires +0.5 cliff offset.");
            }

            int placed = CliffEdgeGenerator.Generate(
                source, cliff, TileSet, Height, ClearBeforeGenerate);

            Debug.Log($"[CliffPlateauGenerator] Mode={PlateauMode}, placed {placed} cliff tiles, height={Height}.");
            return placed;
        }

        void Reset()
        {
            EnsureStandardComponents();
            EnsureCliffChild();
            SyncDualGridOffset();
        }

        void OnValidate()
        {
            if (Height < 1)
            {
                Height = 1;
            }

            SyncDualGridOffset();
        }

        void OnDrawGizmosSelected()
        {
            if (!DrawCliffGizmo)
            {
                return;
            }

            var source = SourceTilemap;
            var cliff = CliffTilemap;
            if (source == null || cliff == null || Height < 1)
            {
                return;
            }

            var grid = source.layoutGrid ?? cliff.layoutGrid;
            if (grid == null)
            {
                return;
            }

            var cellSize = grid.cellSize;
            var cliffBoxSize = cellSize * 0.75f;
            var edgeBoxSize = cellSize * 0.92f;
            var sourceBoxSize = cellSize * 0.55f;
            var drawnTopRowCliff = new HashSet<Vector3Int>();
            var drawnSourceSouthEdge = new HashSet<Vector3Int>();

            foreach (var placement in CliffEdgeGenerator.EnumerateCliffPlacements(source, Height))
            {
                Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.9f);
                Gizmos.DrawWireCube(cliff.GetCellCenterWorld(placement.CliffCell), cliffBoxSize);

                if (placement.IsEdgeRow && drawnSourceSouthEdge.Add(placement.ViewSouthEdgeCell))
                {
                    Gizmos.color = new Color(0.55f, 0.85f, 1f, 0.85f);
                    Gizmos.DrawWireCube(
                        CliffEdgeGenerator.ResolveGizmoViewSouthEdgeWorld(source, placement),
                        sourceBoxSize);
                }

                if (!placement.IsEdgeRow || !drawnTopRowCliff.Add(placement.CliffCell))
                {
                    continue;
                }

                Gizmos.color = ResolveEdgeGizmoColor(placement);
                var edgeWorld = CliffEdgeGenerator.ResolveGizmoCliffNorthEdgeWorld(cliff, placement, cellSize);
                Gizmos.DrawWireCube(edgeWorld, edgeBoxSize);
            }
        }

        static Color ResolveEdgeGizmoColor(CliffEdgeGenerator.CliffPlacement placement)
        {
            if (placement.Attrs.DepthJunction != CliffDepthJunction.None)
            {
                return new Color(0.35f, 0.65f, 1f, 1f);
            }

            return placement.Attrs.Span switch
            {
                CliffSpanRole.LeftEnd => new Color(1f, 0.55f, 0.1f, 1f),
                CliffSpanRole.RightEnd => new Color(1f, 0.35f, 0.55f, 1f),
                CliffSpanRole.Single => new Color(1f, 0.75f, 0.2f, 1f),
                _ => new Color(0.2f, 0.95f, 0.35f, 1f),
            };
        }

        Tilemap ResolveCliffTilemap()
        {
            var cliffTransform = transform.Find(DefaultCliffChildName);
            return cliffTransform != null ? cliffTransform.GetComponent<Tilemap>() : null;
        }

        void ApplyCliffRendererDefaults(TilemapRenderer cliffRenderer)
        {
            if (cliffRenderer == null)
            {
                return;
            }

            var referenceRenderer = ResolveReferenceRenderer();
            if (referenceRenderer == null)
            {
                return;
            }

            cliffRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
            cliffRenderer.sortingOrder = referenceRenderer.sortingOrder - 1;
        }

        TilemapRenderer ResolveReferenceRenderer()
        {
            var dual = ResolveDual();
            if (dual != null)
            {
                if (dual.ViewTilemap != null)
                {
                    var viewRenderer = dual.ViewTilemap.GetComponent<TilemapRenderer>();
                    if (viewRenderer != null)
                    {
                        return viewRenderer;
                    }
                }

                if (dual.DataTilemap != null)
                {
                    return dual.DataTilemap.GetComponent<TilemapRenderer>();
                }
            }

            return GetComponent<TilemapRenderer>();
        }
    }
}
